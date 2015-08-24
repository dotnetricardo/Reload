// Guids.cs
// MUST match guids.h
using System;

namespace ShapeFx.Shape_Package
{
    static class GuidList
    {
        public const string guidShape_PackagePkgString = "9a992702-02cb-4ac4-a16b-07372f08cbc8";
        public const string guidShape_PackageCmdSetString = "9239128b-732b-4f22-a9e4-5e7945514d3f";

        public static readonly Guid guidShape_PackageCmdSet = new Guid(guidShape_PackageCmdSetString);
    };
}