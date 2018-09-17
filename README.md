## GlowingObjectOutlines
A method of rendering Glowing Object Outlines in Unity, using command buffers.

Dead-simple to use. Simply add the GlowObject script to whatever object you want to make glow. That's it.

Well optimized: <0.01ms when inactive (not glowing), <0.1ms when active on 4670k & RX480.

Supports both Forward and Deferred Rendering.

Will make child objects with mesh renderers glow (for ease of use with compound objects).

[Update] I reported the bug to Unity and their QA team has confirmed the bug and sent it to their developers for resolution. Bug Details: The effect breaks in Forward Rendering with MSAA and HDR disabled. It seems that the camera's rendered image isn't stored in `BuiltinRenderTextureType.CameraTarget` when HDR & MSAA are disabled; reading from it returns only black.

Does not support rendering to multiple cameras (as it's intended as a local HUD effect), but should be easy to implement if that's what you want; just put the GlowController on an object separate from the cameras and add the buffer (in GlowController) to any camera that should render the glow (possibly using OnWillRenderObject() to optimize by only adding to cameras that can see a glowing object).


This fork is an optimization of the original repository. Key points of optimization:

- Integrating the composite into the CommandBuffer.
- De-registering to only render objects that are glowing.
- Rebuilding CommandBuffer only on color change (instead of every update).

Optimizing **blur** step is probably the best course of action for further optimization.
