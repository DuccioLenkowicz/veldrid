﻿namespace Veldrid.Graphics
{
    /// <summary>
    /// Describes which kind of function to use when comparing depth values.
    /// </summary>
    public enum DepthComparison
    {
        Never,
        Less,
        Equal,
        LessEqual,
        Greater,
        NotEqual,
        GreaterEqual,
        Always
    }
}
