// Copyright (C)2023 Peter Rundqvist.
// All rights reserved.
// See the LICENSE file in the project root for more information.

using System;
using CommunityToolkit.Mvvm.ComponentModel.Enums;

namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Attribute used to flag a property to be used in the generated DTO class.
/// <param name="roles">The role(s) of property in the generated DTO. The role parameter is a <see cref="DTOMemberRole"/> enumeration that can be bitwize or'ed.</param>
/// <para>
/// This attribute can be used as follows:
/// <code>
/// [DTO]
/// partial class MyEntity
/// {
///     [DTOMember(Roles = DTOMemberRole.Read)]
///     public string Name { get; set; }
///
///     [DTOMember(Roles = DTOMemberRole.Create | DTOMemberRole.Read | DTOMemberRole.Update)]
///     public bool IsEnabled { get; set; }
/// }
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[System.Diagnostics.Conditional("DTOMemberAttribute_DEBUG")]
public sealed class DTOMemberAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the configured DTO member roles for this property.
    /// </summary>
    public DTOMemberRole Roles { get; init; } = DTOMemberRole.Create | DTOMemberRole.Read | DTOMemberRole.Update | DTOMemberRole.Delete;

    
}

