namespace Praxis.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public struct CameraComponent
{
    public bool isOrthographic;
    public float fieldOfView;
    public float near;
    public float far;
    public Color clearColor;
    public ObjectHandle<RenderTarget2D> renderTarget;
    public ObjectHandle<ScreenFilterStack> filterStack;
}