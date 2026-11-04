using System;
using System.IO;
using Godot;
using Qhlc.Core.Nts;

namespace Qhlc.Game;

public partial class LoginController : Control
{
    private static readonly string[] PanelCandidates =
    {
        "STARTUP.bmp",
        "Form_NewRole_3.bmp",
        "Form_changePassWord.bmp",
        "form_deleteRole.bmp",
    };

    [Signal]
    public delegate void EnterGameEventHandler();

    public string AssetsRoot { get; set; } = "";

    public override void _Ready()
    {
        var button = GetNode<Button>("EnterButton");
        button.Pressed += OnEnterPressed;

        if (!TryApplyPanelBackground())
            ShowFallbackBackground();
    }

    private void OnEnterPressed() => EmitSignal(SignalName.EnterGame);

    private bool TryApplyPanelBackground()
    {
        var skinDir = Path.Combine(AssetsRoot, "assets", "pic");
        if (!Directory.Exists(skinDir))
            return false;

        foreach (var name in PanelCandidates)
        {
            var path = Path.Combine(skinDir, name);
            if (!File.Exists(path))
                continue;
            GD.Print(path);
            try
            {
                var bytes = File.ReadAllBytes(path);
                var raster = NtsBmp.Decode(bytes);
                var image = Image.CreateFromData(
                    raster.Width,
                    raster.Height,
                    false,
                    Image.Format.Rgba8,
                    raster.Rgba);
                var texture = ImageTexture.CreateFromImage(image);

                var background = GetNode<TextureRect>("Background");
                background.Texture = texture;
                background.Visible = true;
                GD.Print("加载成功");
                return true;
            }
            catch (Exception ex)
            {
                GD.Print($"Login: BMP load failed ({name}): {ex.Message}");
            }
        }

        return false;
    }

    private void ShowFallbackBackground()
    {
        var background = GetNode<TextureRect>("Background");
        background.Visible = false;

        var fallback = GetNode<ColorRect>("FallbackBackground");
        fallback.Visible = true;
    }
}
