using System.Text;
using System.Xml;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;

namespace Praxis.Core;

public class TextInputWidget : Widget
{
    public event SubmitHandler? OnSubmit;

    public bool IsPassword
    {
        get => _isPassword;
        set
        {
            _isPassword = value;

            if (_isPassword)
            {
                SetPasswordText();
            }
            else
            {
                _textWidget.Text = _text;
            }
        }
    }
    
    public string Text
    {
        get => _text;
        set
        {
            if (value.Contains('\n'))
            {
                value = value.ReplaceLineEndings("");
            }

            _promptWidget.visible = string.IsNullOrEmpty(value);
            _text = value;
            
            if (_isPassword)
            {
                SetPasswordText();
            }
            else
            {
                _textWidget.Text = value;
            }
        }
    }
    
    public string Prompt
    {
        get => _promptWidget.Text;
        set
        {
            _promptWidget.Text = value;
        }
    }

    private readonly TextWidget _textWidget = new()
    {
        inheritVisualState = true,
        tags = [ "input", "input-text" ],
        anchorMax = Vector2.One,
        RichTextEnabled = false,
    };

    private readonly TextWidget _promptWidget = new()
    {
        tint = new Color(255, 255, 255, 128),
        visible = false,
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

    private string _text = "";
    private bool _isPassword = false;

    private StringBuilder _passwordBuilder = new StringBuilder();

    public TextInputWidget() : base()
    {
        tags = [ "input" ];
        AddWidget(_imageWidget);
        AddWidget(_textWidget);
        AddWidget(_promptWidget);
        interactive = true;

        _textWidget.Rtl.CalculateGlyphs = true;
    }

    public override void Deserialize(PraxisGame game, XmlNode node)
    {
        base.Deserialize(game, node);
        
        if (node.Attributes?["password"] is XmlAttribute password)
        {
            IsPassword = bool.Parse(password.Value);
        }
        
        if (node.Attributes?["text"] is XmlAttribute text)
        {
            Text = text.Value;
        }
        else
        {
            Text = "";
        }
        
        if (node.Attributes?["prompt"] is XmlAttribute prompt)
        {
            Prompt = prompt.Value;
        }
    }

    protected override void OnStyleUpdated()
    {
        base.OnStyleUpdated();
        _textWidget.UpdateStyle();
        _promptWidget.UpdateStyle();
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
            switch (direction)
            {
                case NavigationDirection.Left:
                {
                    _cursorPos--;
                    if (_cursorPos <= 0) _cursorPos = 0;

                    _blinkTimer = 0f;
                    break;
                }
                case NavigationDirection.Right:
                {
                    _cursorPos++;
                    if (_cursorPos > Text.Length) _cursorPos = Text.Length;

                    _blinkTimer = 0f;
                    break;
                }
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

        if (VisualState.HasFlags(WidgetState.Focused))
        {
            if (Root!.Game.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                int selectionStart = Math.Min(_cursorPos, _selectionAnchor);
                int selectionEnd = Math.Max(_cursorPos, _selectionAnchor);
                int selectionLength = selectionEnd - selectionStart;
                
                if (Root.Game.CurrentKeyboardState.IsKeyDown(Keys.C) && Root.Game.PreviousKeyboardState.IsKeyUp(Keys.C))
                {
                    // copy
                    if (selectionLength > 0)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectionStart, selectionLength));
                    }
                }
                else if (Root.Game.CurrentKeyboardState.IsKeyDown(Keys.X) && Root.Game.PreviousKeyboardState.IsKeyUp(Keys.X))
                {
                    // cut
                    if (selectionLength > 0)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectionStart, selectionLength));
                        Text = string.Concat(Text.AsSpan(0, selectionStart), Text.AsSpan(selectionEnd));
                        _cursorPos = selectionStart;
                        _selectionAnchor = _cursorPos;
                    }
                }
                else if (Root.Game.CurrentKeyboardState.IsKeyDown(Keys.V) && Root.Game.PreviousKeyboardState.IsKeyUp(Keys.V))
                {
                    // paste
                    if (selectionLength > 0)
                    {
                        Text = string.Concat(Text.AsSpan(0, selectionStart), Text.AsSpan(selectionEnd));
                        _cursorPos = selectionStart;
                    }

                    string insert = SDL.SDL_GetClipboardText();
                    Text = Text.Insert(selectionStart, insert);
                    _cursorPos = selectionStart + insert.Length;
                    _selectionAnchor = _cursorPos;
                }
                else if (Root.Game.CurrentKeyboardState.IsKeyDown(Keys.A) && Root.Game.PreviousKeyboardState.IsKeyUp(Keys.A))
                {
                    // select all
                    _selectionAnchor = 0;
                    _cursorPos = _text.Length;
                }
            }
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
                    if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(CharIndexToGlyphIndex(selectionStart - 1)) is TextChunkGlyph glyph)
                    {
                        startPos = glyph.Bounds.Left + glyph.XAdvance;
                    }
                }

                if (selectionEnd <= _textWidget.Rtl.Lines[0].Count && selectionEnd > 0)
                {
                    if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(CharIndexToGlyphIndex(selectionEnd - 1)) is TextChunkGlyph glyph)
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
                if (_textWidget.Rtl.Lines[0].GetGlyphInfoByIndex(CharIndexToGlyphIndex(_cursorPos - 1)) is TextChunkGlyph glyph)
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
        _promptWidget.DrawInternal(renderer);
        
        renderer.PopClipRect();
    }

    private int CharIndexToGlyphIndex(int index)
    {
        int count = 0;
        for (int i = 0; i < index; i += char.IsSurrogatePair(_text, i) ? 2 : 1)
        {
            count++;
        }

        return count;
    }

    private void OnTextInput(char input)
    {
        if (Root!.Game.CurrentKeyboardState.IsKeyDown(Keys.LeftControl)) return;
        
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
            OnSubmit?.Invoke();
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

    private void SetPasswordText()
    {
        _passwordBuilder.Clear();

        for (int i = 0; i < _text.Length; i++)
        {
            _passwordBuilder.Append('•');
        }

        _textWidget.Text = _passwordBuilder.ToString();
    }
}