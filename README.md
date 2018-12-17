## GlowingObjectOutlines
A method of rendering Glowing Object Outlines in Unity, using command buffers.

Simple to use: Add the GlowObject script to whatever object you want to make glow and add the GlowController script to the camera that uses the effect (GlowController will lazy-load onto Camera.main if you forget adding it to the scene).

Well optimized: <0.01ms when inactive (not glowing), ~0.1ms when active on 4670K & RX480.

Will make child objects with mesh renderers glow (for ease of use with compound objects).

Supports Deferred and Forward with HDR and/or MSAA. But the effect breaks in Forward Rendering **with MSAA and HDR disabled**. The camera's rendered image isn't stored in BuiltinRenderTextureType.CameraTarget when HDR & MSAA are disabled. I don't know of any way to access it from inside a command buffer, but if you have that specific use-case and are ok with the performance hit you can make it work by breaking out the composite into OnRenderImage, like so:

```cs
void OnRenderImage(RenderTexture src, RenderTexture dst){
	Graphics.Blit(src, dst, compositeShader, 0);
}
```

Does not support rendering to multiple cameras (as it's intended as a local HUD effect), but should be easy to implement if that's what you want; just put the GlowController on an object separate from the cameras and add the buffer (in GlowController) to any camera that should render the glow.


This fork is an optimization of the original repository. A number of points have been improved, main two being that only actively glowing objects are rendered and that the effect deactivates when nothing is glowing, effectively costing zero performance.

Optimizing **blur** step is probably the best course of action for further optimization.
