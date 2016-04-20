﻿using System.Collections.Generic;
using System.Numerics;

namespace Veldrid.Platform
{
    public interface InputSnapshot
    {
        IReadOnlyCollection<KeyEvent> KeyEvents { get; }
        IReadOnlyCollection<MouseEvent> MouseEvents { get; }
        IReadOnlyCollection<char> KeyCharPresses { get; }
        Vector2 MousePosition { get; }
    }
}