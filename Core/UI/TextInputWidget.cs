using System.Xml;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Praxis.Core;

public class TextInputWidget : Widget
{
    public string Text
    {
        get => _textWidget.Text;
        set
        {
            if (value.Contains('\n'))
            {
                _textWidget.Text = value.ReplaceLineEndings("");
            }
            else
            {
                _textWidget.Text = value;
            }
        }
    }

    private readonly TextWidget _textWidget = new()
    {
        inheritVisualState = true,
        tags = [ "input", "input-text" ],
        anchorMax = Vector2.One,
        RichTextEnabled = false,
    };

    private readonly ImageWidget _imageWidget = new()
    {
        inheritVisualState = true,
        tags = [ "input", "input-bg" ],
        anchorMax = Vector2.One
    };

    private int _cursorPos = 0;
    private int _selectionAnchor = 0;
    private float _blinkTimer = 0f;

    private int _scrollOffset = 0;

    public TextInputWidget() : base()
    {
        tags = [ "input" ];
        AddWidget(_imageWidget);
        AddWidget(_textWidget);
        interactive = true;

        _textWidget.Rtl.CalculateGlyphs = true;
    }

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);

        if (node.Attributes?["text"] is XmlAttribute text)
        {
            Text = text.Value;
        }
    }

    protected override void OnStyleUpdated()
    {
        base.OnStyleUpdated();
        _textWidget.UpdateStyle();
        _imageWidget.UpdateStyle();
    }

    public override void HandleFocusGained()
    {
        base.HandleFocusGained();
        TextInputEXT.TextInput += OnTextInput;
        TextInputEXT.TextEditing += OnTextEditing;
        TextInputEXT.StartTextInput();
        _selectionAnchor = 0;
        _cursorPos = Text.Length;
        _blinkTimer = 0f;
    }

    public override void HandleFocusLost()
    {
        base.HandleFocusLost();
        TextInputEXT.TextInput -= OnTextInput;
        TextInputEXT.TextEditing -= OnTextEditing;
        TextInputEXT.StopTextInput();
    }

    public override void HandleNavigation(NavigationDirection direction)
    {
        if (VisualState.HasFlags(WidgetState.Focused))
        {
            if (direction == NavigationDirection.Left)
            {
                _cursorPos--;
                if (_cursorPos <= 0) _cursorPos = 0;

                _blinkTimer = 0f;
            }
            else if (direction == NavigationDirection.Right)
            {
                _cursorPos++;
                if (_cursorPos > Text.Length) _cursorPos = Text.Length;

                _blinkTimer = 0f;
            }

            if (!Root!.Game.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
            {
                _selectionAnchor = _cursorPos;
            }
        }
        else
        {
            base.HandleNavigation(direction);
        }
    }

    public override void Update(UIRenderer renderer, float deltaTime)
    {
        base.Update(renderer, deltaTime);
        _blinkTimer += deltaTime;
        if (_blinkTimer >= 1f)
        {
            _blinkTimer = 0f;
        }
    }

    protected override void Draw(UIRenderer renderer, Rectangle rect)
    {
        Rectangle contentRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        contentRect.X += _textWidget.WidgetStyle.Padding?.Left ?? 0;
        contentRect.Y += _textWidget.WidgetStyle.Padding?.Top ?? 0;
        contentRect.Height -= (_textWidget.WidgetStyle.Padding?.Top ?? 0) + (_textWidget.WidgetStyle.Padding?.Bottom ?? 0);
        contentRect.Width -= (_textWidget.WidgetStyle.Padding?.Left ?? 0) + (_textWidget.WidgetStyle.Padding?.Right ?? 0);
        
        _imageWidget.DrawInternal(renderer);
        
        renderer.PushClipRect(contentRect);

        int selectionStart = Math.Min(_cursorPos, _selectionAnchor);
        int selectionEnd = Math.Max(_cursorPos, _selectionAnchor);
        int selectionLength = selectionEnd - selectionStart;

        if (VisualState.HasFlags(WidgetState.Focused) && selectionLength > 0)
        {
            Rectangle selectionRect = contentRect;

            int startPos = 0;
            int endPos = 0;

            if (_textWidget.Rtl.Lines.Count > 0)
            {
                if (selectionStart <= _textWidget.Rtl.Lines[0].Count && selectionStart > 0)
                {
                    if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(selectionStart - 1) is TextChunkGlyph glyph)
                    {
                        startPos = glyph.Bounds.Left + glyph.XAdvance;
                    }
                }

                if (selectionEnd <= _textWidget.Rtl.Lines[0].Count && selectionEnd > 0)
                {
                    if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(selectionEnd - 1) is TextChunkGlyph glyph)
                    {
                        endPos = glyph.Bounds.Left + glyph.XAdvance;
                    }
                }
            }

            selectionRect.X += startPos;
            selectionRect.Width = endPos - startPos;

            selectionRect.X += _scrollOffset;

            renderer.DrawRect(selectionRect, Color.Blue);
        }
        
        if (VisualState.HasFlags(WidgetState.Focused))
        {
            Rectangle cursorRect = new Rectangle(contentRect.X, contentRect.Y, 1, contentRect.Height);

            if (_textWidget.Rtl.Lines.Count > 0 && _cursorPos <= _textWidget.Rtl.Lines[0].Count && _cursorPos > 0)
            {
                if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(_cursorPos - 1) is TextChunkGlyph glyph)
                {
                    cursorRect.X += glyph.Bounds.Left + glyph.XAdvance;
                }
            }

            int prevScrollOffset = _scrollOffset;

            if (_textWidget.Rtl.Lines.Count > 0 && _textWidget.Rtl.Lines[0].Size.X <= contentRect.Width)
            {
                _scrollOffset = 0;
            }
            else
            {
                if (cursorRect.Left < (contentRect.Left - _scrollOffset))
                {
                    _scrollOffset = contentRect.Left - cursorRect.Left;
                }
                else if (cursorRect.Right > (contentRect.Right - _scrollOffset))
                {
                    _scrollOffset = contentRect.Right - cursorRect.Right;
                }
            }

            if (_scrollOffset != prevScrollOffset)
            {
                _textWidget.anchoredPosition = new Vector2(_scrollOffset, 0f);
                _textWidget.Update(renderer, 0f);
            }

            cursorRect.X += _scrollOffset;

            if (_blinkTimer <= 0.5f)
            {
                renderer.DrawRect(cursorRect, _textWidget.WidgetStyle.TextColor ?? Color.White);
            }
        }

        _textWidget.DrawInternal(renderer);
        
        renderer.PopClipRect();
    }

    private void OnTextInput(char input)
    {
        int selectionStart = Math.Min(_cursorPos, _selectionAnchor);
        int selectionEnd = Math.Max(_cursorPos, _selectionAnchor);
        int selectionLength = selectionEnd - selectionStart;

        if (selectionLength > 0)
        {
            Text = string.Concat(Text.AsSpan(0, selectionStart), Text.AsSpan(selectionEnd));
            _cursorPos = selectionStart;
        }
        
        if (input == '\b')
        {
            if (_cursorPos > 0 && selectionLength == 0)
            {
                Text = string.Concat(Text.AsSpan(0, _cursorPos - 1), Text.AsSpan(_cursorPos));
                _cursorPos--;
            }
        }
        else if (input == 127)
        {
            if (_cursorPos < Text.Length && selectionLength == 0)
            {
                Text = string.Concat(Text.AsSpan(0, _cursorPos), Text.AsSpan(_cursorPos + 1));
            }
        }
        else if (input == '\r' || input == '\n')
        {
            // todo: OnSubmit
            Root?.SetFocus(null);
        }
        else
        {
            Text = Text.Insert(_cursorPos, $"{input}");
            _cursorPos++;
        }

        _selectionAnchor = _cursorPos;
        _blinkTimer = 0f;
    }

    private void OnTextEditing(string composition, int cursor, int length)
    {
        Text = composition;
        _selectionAnchor = cursor;
        _cursorPos = cursor + length;
    }
}