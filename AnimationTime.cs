namespace InvernaMapStudio;

static class AnimationTime
{
    internal static int FrameIndex(TimeSpan elapsed, TimeSpan duration, int count)
    {
        if (elapsed < TimeSpan.Zero || duration <= TimeSpan.Zero || count <= 0) throw new ArgumentOutOfRangeException();
        return (int)((elapsed.Ticks / duration.Ticks) % count);
    }

    internal static void SelfTest()
    {
        var duration = TimeSpan.FromMilliseconds(140);
        if (FrameIndex(TimeSpan.FromMilliseconds(139), duration, 4) != 0 || FrameIndex(duration, duration, 4) != 1 || FrameIndex(TimeSpan.FromMilliseconds(560), duration, 4) != 0)
            throw new Exception("Sprite animation boundaries failed.");
        // Presentation callbacks may be missed during dragging, resizing or an
        // expensive paint. A late callback must select the current pose directly.
        foreach (var fps in new[] { 30, 60, 120, 144 })
        {
            var last = 0;
            for (var i = 0; i <= fps * 3; i++) last = FrameIndex(TimeSpan.FromSeconds(i / (double)fps), duration, 7);
            if (last != 0 || FrameIndex(TimeSpan.FromSeconds(3), duration, 7) != last) throw new Exception("Callback rate changed the sprite timeline.");
        }
        if (FrameIndex(TimeSpan.FromSeconds(10), duration, 7) != 1) throw new Exception("Animation did not skip missed presentation frames.");
    }
}
