using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// Creates and maintains a command buffer for the glow effect.
/// </summary>
public sealed class GlowController : MonoBehaviour {

	public float GlowIntensity { set { compositeShader.SetFloat(intensityID, value); } }
	[Tooltip("Start value only. Cannot be changed in editor during runtime, but change through public setter works during runtime.")] //Pasting void Update(){GlowIntensity = glowIntensity;} somewhere in this class will allow testing the value easier, just remove it after for performance
	[SerializeField][Range(0f, 10f)] float glowIntensity = 4f;

	CommandBuffer glowBuff;
	List<GlowObject> glowObjects = new List<GlowObject>();
	Material glowShader, blurShader, compositeShader;
	Vector2 blurTexelSize;

	int prePassRTID, blurPassRTID, tempRTID;
	int blurSizeID;
	int glowColID;
	int intensityID;
	int lastRebuildFrame;

#region Singleton
	private static GlowController instance;
	public static GlowController Inst {
		get {
			if (instance == null){ //Lazy-load object or create it in case somebody forgot to add it to the scene
				Camera.main.gameObject.AddComponent<GlowController>(); //AddComponent runs awake function before continuing
			}
			return instance;
		}
	}
	void Awake(){
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);
#endregion
		//Cache shaders and shader properties and setup command buffer to be called on a camera event (certain place in rendering pipeline)
		glowShader = new Material(Shader.Find("Hidden/GlowShader"));
		blurShader = new Material(Shader.Find("Hidden/Blur"));
		compositeShader = new Material(Shader.Find("Hidden/GlowComposite"));

		prePassRTID = Shader.PropertyToID("_GlowPrePassTex");
		blurPassRTID = Shader.PropertyToID("_GlowBlurredTex");
		tempRTID = Shader.PropertyToID("_TempTex0");
		blurSizeID = Shader.PropertyToID("_BlurSize");
		glowColID = Shader.PropertyToID("_GlowColor");

		intensityID = Shader.PropertyToID("_Intensity");
		compositeShader.SetFloat(intensityID, glowIntensity);

		glowBuff = new CommandBuffer();
		glowBuff.name = "Glowing Objects Buffer"; //Visible in Frame Debugger
		GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeImageEffects, glowBuff); //Injection-point in rendering pipeline. Pipeline info can be found here: https://docs.unity3d.com/Manual/GraphicsCommandBuffers.html
	}

	/// <summary>
	/// Add object to list of glowing objects to be rendered.
	/// </summary>
	public void RegisterObject(GlowObject _glowObj){
		if (!glowObjects.Contains(_glowObj)) //Inefficient list search, could probably optimize to be prevented at an object-level. My attempt checked script enabled status on object before running this method, but resulted in weird activation bugs
			glowObjects.Add(_glowObj);
	}
	/// <summary>
	/// Remove object from list of glowing objects to be rendered. Updates (rebuilds) buffer.
	/// </summary>
	public void DeRegisterObject(GlowObject _glowObj){
		glowObjects.Remove(_glowObj);
		if (glowObjects.Count < 1) //Clearing for (almost)zero overhead when there's no active glow
			glowBuff.Clear();
		else
			RebuildCommandBuffer();
	}

	/// <summary>
	/// Adds commands to the command buffer (commands execute in order).
	/// Only needs to rebuild on color change.
	/// </summary>
	public void RebuildCommandBuffer(){
		int frameCount = Time.frameCount; //Simple optimization to only rebuild once per frame for when several objects are updating color at the same time.
		if (lastRebuildFrame == frameCount)
			return;
		else
			lastRebuildFrame = frameCount;
		
		glowBuff.Clear();
		//Set shader color and render only the objects that should glow to a TRT (TemporaryRenderTexture)
		glowBuff.GetTemporaryRT(prePassRTID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.Default); //-1 height/width = screen height/width.
		glowBuff.SetRenderTarget(prePassRTID);
		glowBuff.ClearRenderTarget(true, true, Color.clear);
		for (int i = 0, len = glowObjects.Count; i < len; i++){
			glowBuff.SetGlobalColor(glowColID, glowObjects[i].CurrentColor);

			for (int j = 0; j < glowObjects[i].Renderers.Length; j++)
				glowBuff.DrawRenderer(glowObjects[i].Renderers[j], glowShader);
		}
		//Use lower-res RTs for higher performance (-2 width = Screen.width >> 1, i.e: bit-shift, dividing the value by 2)
		glowBuff.GetTemporaryRT(tempRTID, -2, -2, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
		glowBuff.GetTemporaryRT(blurPassRTID, -2, -2, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
		glowBuff.Blit(prePassRTID, blurPassRTID); //Copy RT with colored render of objects to new lower res RT
		blurTexelSize = new Vector2(1.5f / (Screen.width >> 1), 1.5f / (Screen.height >> 1));
		glowBuff.SetGlobalVector(blurSizeID, blurTexelSize);
		//Run blur passes
		for (int i = 0; i < 4; i++){
			glowBuff.Blit(blurPassRTID, tempRTID, blurShader, 0); //Blur horizontal
			glowBuff.Blit(tempRTID, blurPassRTID, blurShader, 1); //Blur vertical
		}
		//Subtract prePassRT(shape) from blurPassRT(shape+"outline") and add only the outline to final rendered image
		glowBuff.ReleaseTemporaryRT(tempRTID);
		glowBuff.GetTemporaryRT(tempRTID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR); //HDR color needed for effects that rely on it (like bloom)
		glowBuff.Blit(BuiltinRenderTextureType.CameraTarget, tempRTID); //Can't blit to and from the same RT (CameraTarget) with deferred rendering, so an intermediary is needed.
		glowBuff.Blit(tempRTID, BuiltinRenderTextureType.CameraTarget, compositeShader, 0);
		//Release RTs for potential re-use by other shaders (temporary RTs are pooled by Unity)
		glowBuff.ReleaseTemporaryRT(tempRTID);
		glowBuff.ReleaseTemporaryRT(blurPassRTID);
		glowBuff.ReleaseTemporaryRT(prePassRTID);
	}
}


//Could possibly be optimized to use DrawMesh and sending in a reference to a MaterialPropertyBlock(with color data)
//instead of DrawRenderer, which would allow buffer to only rebuild when adding/removing objects from glowObjects list.
//With the GlowObject scripts updating the color data in their MaterialPropertyBlocks locally.
//Would probably also require updating a matrix for position, rotation and scale information:
//	matrix[i] = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);
//Where trans is a reference to the respective child transforms (if you want child objects to glow as well, you don't even need to make an array otherwise).
//And this replacing the current contents of the glowObjects loop:
//		for (int j = 0; j < glowObjects[i].Meshes.Length; j++){
//			glowBuff.DrawMesh(glowObjects[i].meshes[j], glowObjects[i].matrix[j], glowShader, 0, 0, glowObjects[i].matPropBlock[j]);
//		}
//But having to manually update matrix information like this would probably not help overall performance,
//so look at it more like an experiment if you're interested in trying it out.

//Optimizing the blur shader is probably the best course of action for further optimization.