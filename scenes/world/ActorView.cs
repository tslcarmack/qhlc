using Godot;
using Qhlc.Core.Nts;
using Qhlc.Core.Public;
using Qhlc.Player.Public;

namespace Qhlc.Game;

/// <summary>
/// Thin presenter: Character foot position + <see cref="ActorAnimator"/> frame onto a sprite.
/// Does not own equip or pathfinding.
/// </summary>
public sealed class ActorView
{
    /// <summary>002-family measured foot line above entity origin (tsdocs §05.1).</summary>
    private const int FootOffsetY = 29;

    private readonly Node2D _root;
    private readonly Sprite2D _sprite;
    private readonly ActorAnimator _animator = new();
    private string _assetsRoot = "";
    private int _spriteW;
    private int _spriteH;

    public ActorView(Node2D root, Sprite2D sprite)
    {
        _root = root;
        _sprite = sprite;
    }

    public void BindAssetsRoot(string assetsRoot) => _assetsRoot = assetsRoot;

    public void Prime(Character character)
    {
        _animator.NotifyFacing(character.Facing);
        Tick(0, character);
    }

    /// <summary>Re-synthesize sprite after logical equip without touching pathfinding.</summary>
    public void RefreshAfterEquip(Character character)
    {
        _animator.InvalidateAppearanceCache();
        _animator.NotifyFacing(character.Facing);
        Tick(0, character);
    }

    public void Tick(double delta, Character character)
    {
        if (character.Behavior == BehaviorState.Walk)
            _animator.NotifyFacing(character.Facing);

        _animator.Tick(delta, character);

        _animator.TryGetFrame(_assetsRoot, character, out var raster, out var originX, out var originY);
        if (raster is not null)
            ApplyFrame(raster, originX, originY);

        SyncTransform(character, _lastOriginX, _lastOriginY);
    }

    private int _lastOriginX;
    private int _lastOriginY;

    public void SyncTransform(Character character)
    {
        SyncTransform(character, _lastOriginX, _lastOriginY);
    }

    public void SyncCamera(Camera2D camera, Character character)
    {
        camera.Position = new Vector2(character.FootX, character.FootY - FootOffsetY);
    }

    private void ApplyFrame(NtsRaster raster, int originX, int originY)
    {
        _lastOriginX = originX;
        _lastOriginY = originY;
        _sprite.Texture = RasterToTexture(raster);
        _sprite.Centered = false;
        _spriteW = raster.Width;
        _spriteH = raster.Height;
    }

    private void SyncTransform(Character character, int originX, int originY)
    {
        _root.Position = new Vector2(character.FootX, character.FootY);
        if (_spriteW > 0 && _spriteH > 0)
        {
            _sprite.Position = new Vector2(
                -originX,
                FootOffsetY - originY);
        }
    }

    private static ImageTexture RasterToTexture(NtsRaster raster)
    {
        var image = Godot.Image.CreateFromData(
            raster.Width,
            raster.Height,
            false,
            Godot.Image.Format.Rgba8,
            raster.Rgba);
        return ImageTexture.CreateFromImage(image);
    }
}
