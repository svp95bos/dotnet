using System;
namespace CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// An attribute that indicates that a given type is subject to
/// being used for generating Data Transfer Objects.
/// To indicate a class member be included in the DTO,
/// apply the <see cref="DTOMemberAttribute"/> on relevant members.
/// <para>
/// This attribute can be used as follows:
/// <code>
/// [DTO]
/// partial class MyViewModel : SomeOtherClass
/// {
///     // Other members here...
/// }
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DTOAttribute : Attribute
{
}

