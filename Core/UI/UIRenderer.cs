using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Praxis.Core;

/// <summary>
/// Helper class for rendering UIs
/// </summary>
public class UIRenderer : IDisposable
{
    private const int MAX_NEST = 8;

    public readonly GraphicsDevice GraphicsDevice;

    private SpriteBatch _sb;
    private Matrix _activeMatrix = Matrix.Identity;
    private Stack<Matrix> _transformStack = new Stack<Matrix>();
    private int _activeClipLevel;

    private DepthStencilState _currentDsState;
    private DepthStencilState[] _parentDsState;
    private DepthStencilState[] _childDsState;

    private Texture2D _dummy;

    public UIRenderer(GraphicsDevice graphicsDevice)
    {
        _sb = new SpriteBatch(graphicsDevice);
        _dummy = new Texture2D(graphicsDevice, 1, 1);
        _dummy.SetData([new Color(0, 0, 0, 0)]);

        GraphicsDevice = graphicsDevice;

        _parentDsState = new DepthStencilState[MAX_NEST + 1];
        _childDsState = new DepthStencilState[MAX_NEST + 1];
        _parentDsState[0] = DepthStencilState.None;
        _childDsState[0] = DepthStencilState.None;
        int prevMask = 0;
        int writeMask = 0;
        for (int i = 1; i <= MAX_NEST; i++)
        {
            int mask = 1 << (i - 1);
            writeMask |= mask;
            _parentDsState[i] = new DepthStencilState
            {
                DepthBufferEnable = false,
                DepthBufferWriteEnable = false,
                StencilEnable = true,
                StencilFunction = CompareFunction.Equal,
                StencilMask = prevMask,
                StencilWriteMask = writeMask,
                ReferenceStencil = 0xFF,
                StencilPass = StencilOperation.Replace,
                StencilFail = StencilOperation.Keep
            };
            _childDsState[i] = new DepthStencilState
            {
                DepthBufferEnable = false,
                DepthBufferWriteEnable = false,
                StencilEnable = true,
                StencilFunction = CompareFunction.Equal,
                StencilMask = mask,
                StencilWriteMask = 0,
                ReferenceStencil = 0xFF,
                StencilPass = StencilOperation.Keep,
                StencilFail = StencilOperation.Keep
            };
            prevMask = mask;
        }

        _currentDsState = _parentDsState[0];
    }

    public void Begin()
    {
        _activeClipLevel = 0;
        _currentDsState = _parentDsState[0];
        _activeMatrix = Matrix.Identity;
        _transformStack.Clear();
        BeginBatch();
    }

    public void End()
    {
        _sb.End();
    }

    public void PushMatrix(Matrix transform)
    {
        _transformStack.Push(_activeMatrix);
        _activeMatrix = transform * _activeMatrix;
        _sb.End();
        BeginBatch();
    }

    public void PopMatrix()
    {
        if (_transformStack.Count == 0)
        {
            throw new Exception("Tried to pop more transforms than were pushed");
        }

        _activeMatrix = _transformStack.Pop();
        _sb.End();
        BeginBatch();
    }

    public void PushClipRect(Rectangle rect)
    {
        if (_activeClipLevel == MAX_NEST)
        {
            throw new Exception("Exceeded max clipping level of 8");
        }

        _activeClipLevel++;
        _currentDsState = _parentDsState[_activeClipLevel];
        _sb.End();
        BeginBatch();

        // draw clip rect into stencil buffer
        Draw(_dummy, rect, null, Color.White);

        _currentDsState = _childDsState[_activeClipLevel];
        _sb.End();
        BeginBatch();
    }

    public void PopClipRect()
    {
        if (_activeClipLevel == 0)
        {
            throw new Exception("Tried to pop more clip rects than were pushed");
        }

        _activeClipLevel--;
        _currentDsState = _childDsState[_activeClipLevel];
        _sb.End();
        BeginBatch();
    }

    public void Draw(Texture2D texture, Rectangle destRect, Rectangle? sourceRect, Color tint)
    {
        _sb.Draw(texture, destRect, sourceRect, tint);
    }

    public void DrawString(RichTextLayout rtl, Vector2 position, Color tint, TextHorizontalAlignment alignment = TextHorizontalAlignment.Left)
    {
        rtl.Draw(_sb, position, tint, 0f, default, null, 0f, alignment);
    }

    public void Dispose()
    {
        _sb.Dispose();
        _dummy.Dispose();
    }

    private void BeginBatch()
    {
        _sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, _currentDsState, RasterizerState.CullNone, null, _activeMatrix);
    }
}
