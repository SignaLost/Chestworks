namespace Chestworks.Common;

/// <summary>What the padlock button looks like: a square copy of the game's Take All button, or of its weight tab.</summary>
internal enum LockButtonStyle
{
    Button,
    Tab
}

/// <summary>What the padlock button, as a button, is placed against on the chest panel.</summary>
internal enum LockButtonAnchor
{
    TakeAll,
    PlaceStacks,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>Which edge of the chest panel the padlock tab hangs from, and where along it, as the weight tab hangs from the right.</summary>
internal enum LockTabAnchor
{
    RightTop,
    RightBottom,
    LeftTop,
    LeftBottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
