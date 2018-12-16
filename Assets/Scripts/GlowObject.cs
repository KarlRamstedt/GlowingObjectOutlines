using UnityEngine;

public class GlowObject : MonoBehaviour {

	public Color GlowColor = Color.white;

	public Renderer[] Renderers	{ get; private set;	}

	bool active = false;

	void Awake(){
		Renderers = GetComponentsInChildren<Renderer>();
	}

	void OnMouseEnter(){
		EnableGlow();
	}
	void OnMouseExit(){
		DisableGlow();
	}
	public void EnableGlow(){
		if (active)
			return;
		active = true;
		GlowController.Inst.RegisterObject(this);
	}
	public void DisableGlow(){
		if (!active)
			return;
		active = false;
		GlowController.Inst.DeRegisterObject(this);
	}
}
