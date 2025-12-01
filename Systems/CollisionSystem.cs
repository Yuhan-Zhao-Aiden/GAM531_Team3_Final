using OpenTK.Mathematics;

namespace knight.Systems;

public static class CollisionSystem
{
    /// <summary>
    /// Checks if two axis-aligned bounding boxes (AABB) collide.
    /// </summary>
    /// <param name="pos1">Center position of first entity</param>
    /// <param name="size1">Size of first entity</param>
    /// <param name="pos2">Center position of second entity</param>
    /// <param name="size2">Size of second entity</param>
    /// <returns>True if the two entities are overlapping</returns>
    public static bool CheckAABBCollision(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2)
    {
        var halfSize1 = size1 * 0.5f;
        var halfSize2 = size2 * 0.5f;

        var left1 = pos1.X - halfSize1.X;
        var right1 = pos1.X + halfSize1.X;
        var bottom1 = pos1.Y - halfSize1.Y;
        var top1 = pos1.Y + halfSize1.Y;

        var left2 = pos2.X - halfSize2.X;
        var right2 = pos2.X + halfSize2.X;
        var bottom2 = pos2.Y - halfSize2.Y;
        var top2 = pos2.Y + halfSize2.Y;

        return !(right1 < left2 || left1 > right2 || top1 < bottom2 || bottom1 > top2);
    }
}
