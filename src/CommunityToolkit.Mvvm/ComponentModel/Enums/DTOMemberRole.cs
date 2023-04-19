// Copyright (c)2023 Peter Rundqvist.
// All rights reserved.
// See the LICENSE file in the project root for more information.

using System;
namespace CommunityToolkit.Mvvm.ComponentModel.Enums;

[Flags]
public enum DTOMemberRole : uint
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8
}

