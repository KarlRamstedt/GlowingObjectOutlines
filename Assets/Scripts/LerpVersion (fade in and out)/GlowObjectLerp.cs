using UnityEngine;

public class GlowObjectLerp : MonoBehaviour {

	public Color GlowColor = Color.white;
	[Range(0.1f, 99f)] public float LerpFactor = 9f; //Range-limited to usable values

	public Renderer[] Renderers	{ get; private set;	}
	public Color CurrentColor { get { return currentColor; } }

	Color currentColor;
	Color targetColor;

	void Start(){
		Renderers = GetComponentsInChildren<Renderer>();
		enabled = false; //No reason to run unless activated
	}

	void OnMouseEnter(){
		EnableGlow();
	}
	void OnMouseExit(){
		DisableGlow();
	}
	public void EnableGlow(){
		enabled = true;
		targetColor = GlowColor;
		GlowControllerLerp.Inst.RegisterObject(this);
	}
	public void DisableGlow(){
		enabled = true;
		targetColor = Color.black; //Black is transparent for the glow shader
	}

	void Update(){ //Update color, disable script if it reaches target color
		currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * LerpFactor);
		if (currentColor == targetColor){
			if (targetColor == Color.black)
				GlowControllerLerp.Inst.DeRegisterObject(this);
			else
				GlowControllerLerp.Inst.RebuildCommandBuffer(); //Rebuild at final color update if glowing
			enabled = false;
		} else
			GlowControllerLerp.Inst.RebuildCommandBuffer();
	}
}