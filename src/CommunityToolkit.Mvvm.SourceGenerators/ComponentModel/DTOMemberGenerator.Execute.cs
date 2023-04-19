using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.SourceGenerators.ComponentModel.Models;
using CommunityToolkit.Mvvm.SourceGenerators.Extensions;
using CommunityToolkit.Mvvm.SourceGenerators.Helpers;
using CommunityToolkit.Mvvm.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CommunityToolkit.Mvvm.SourceGenerators.Diagnostics.DiagnosticDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CommunityToolkit.Mvvm.SourceGenerators;

partial class DTOMemberGenerator
{
    /// <summary>
    /// A container for all the logic for <see cref="DTOMemberGenerator"/>.
    /// </summary>
    internal static class Execute
    {
        /// <summary>
        /// Processes a given field.
        /// </summary>
        /// <param name="fieldSyntax">The <see cref="PropertyDeclarationSyntax"/> instance to process.</param>
        /// <param name="fieldSymbol">The input <see cref="IFieldSymbol"/> instance to process.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> instance for the current run.</param>
        /// <param name="token">The cancellation token for the current operation.</param>
        /// <param name="propertyInfo">The resulting <see cref="PropertyInfo"/> value, if successfully retrieved.</param>
        /// <param name="diagnostics">The resulting diagnostics from the processing operation.</param>
        /// <returns>The resulting <see cref="PropertyInfo"/> instance for <paramref name="fieldSymbol"/>, if successful.</returns>
        public static bool TryGetInfo(
            PropertyDeclarationSyntax fieldSyntax,
            IPropertySymbol fieldSymbol,
            SemanticModel semanticModel,
            CancellationToken token,
            [NotNullWhen(true)] out PropertyInfo? propertyInfo,
            out ImmutableArray<DiagnosticInfo> diagnostics)
        {
            using ImmutableArrayBuilder<DiagnosticInfo> builder = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

            // Validate the target type
            if (!IsTargetTypeValid(fieldSymbol, out bool shouldAddToDTO))
            {
                builder.Add(
                    InvalidContainingTypeForObservablePropertyFieldError,
                    fieldSymbol,
                    fieldSymbol.ContainingType,
                    fieldSymbol.Name);

                propertyInfo = null;
                diagnostics = builder.ToImmutable();

                return false;
            }

            // Get the property type and name
            string typeNameWithNullabilityAnnotations = fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
            string fieldName = fieldSymbol.Name;
            string propertyName = GetGeneratedPropertyName(fieldSymbol);

            // We skip this since we are working with properties themselves.
            // Check for name collisions
            //if (fieldName == propertyName)
            //{
            //    builder.Add(
            //        ObservablePropertyNameCollisionError,
            //        fieldSymbol,
            //        fieldSymbol.ContainingType,
            //        fieldSymbol.Name);

            //    propertyInfo = null;
            //    diagnostics = builder.ToImmutable();

            //    // If the generated property would collide, skip generating it entirely. This makes sure that
            //    // users only get the helpful diagnostic about the collision, and not the normal compiler error
            //    // about a definition for "Property" already existing on the target type, which might be confusing.
            //    return false;
            //}

            // Skip
            // Check for special cases that are explicitly not allowed
            //if (IsGeneratedPropertyInvalid(propertyName, fieldSymbol.Type))
            //{
            //    builder.Add(
            //        InvalidObservablePropertyError,
            //        fieldSymbol,
            //        fieldSymbol.ContainingType,
            //        fieldSymbol.Name);

            //    propertyInfo = null;
            //    diagnostics = builder.ToImmutable();

            //    return false;
            //}

            using ImmutableArrayBuilder<string> propertyChangedNames = ImmutableArrayBuilder<string>.Rent();
            using ImmutableArrayBuilder<string> propertyChangingNames = ImmutableArrayBuilder<string>.Rent();
            using ImmutableArrayBuilder<string> notifiedCommandNames = ImmutableArrayBuilder<string>.Rent();
            using ImmutableArrayBuilder<AttributeInfo> forwardedAttributes = ImmutableArrayBuilder<AttributeInfo>.Rent();

            bool notifyRecipients = false;
            bool notifyDataErrorInfo = false;
            bool hasOrInheritsClassLevelNotifyPropertyChangedRecipients = false;
            bool hasOrInheritsClassLevelNotifyDataErrorInfo = false;
            bool hasAnyValidationAttributes = false;
            bool isOldPropertyValueDirectlyReferenced = false; //Skip IsOldPropertyValueDirectlyReferenced(fieldSymbol, propertyName);

            // Get the nullability info for the property
            GetNullabilityInfo(
                fieldSymbol,
                semanticModel,
                out bool isReferenceTypeOrUnconstraindTypeParameter,
                out bool includeMemberNotNullOnSetAccessor);
        }

        /// <summary>
        /// Validates the containing type for a given property being annotated.
        /// </summary>
        /// <param name="propertySymbol">The input <see cref="IPropertySymbol"/> instance to process.</param>
        /// <param name="shouldAddToDTO">Whether or not property should be added to a DTO.</param>
        /// <returns>Whether or not the containing type for <paramref name="propertySymbol"/> is valid.</returns>
        private static bool IsTargetTypeValid(IPropertySymbol propertySymbol, out bool shouldAddToDTO)
        {
            // The [DTOMember] attribute can only be used in types that are known to expose the necessary OnPropertyChanged and OnPropertyChanging methods.
            // That means that the containing type for the property needs to match the following condition:
            //   - It has the [DTO] attribute (on itself or any of its base types).
            //bool isObservableObject = fieldSymbol.ContainingType.InheritsFromFullyQualifiedMetadataName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
            bool hasDTOAttribute = propertySymbol.ContainingType.HasOrInheritsAttributeWithFullyQualifiedMetadataName("CommunityToolkit.Mvvm.ComponentModel.DTOAttribute");
            //bool hasINotifyPropertyChangedAttribute = fieldSymbol.ContainingType.HasOrInheritsAttributeWithFullyQualifiedMetadataName("CommunityToolkit.Mvvm.ComponentModel.INotifyPropertyChangedAttribute");

            shouldAddToDTO = hasDTOAttribute;

            return hasDTOAttribute;
        }

        /// <summary>
        /// Get the generated property name for an input property.
        /// </summary>
        /// <param name="propertySymbol">The input <see cref="IPropertySymbol"/> instance to process.</param>
        /// <returns>The generated property name for <paramref name="propertySymbol"/>.</returns>
        public static string GetGeneratedPropertyName(IPropertySymbol propertySymbol)
        {
            string propertyName = propertySymbol.Name;

            return propertyName;
        }

        /// <summary>
        /// Checks whether the generated code has to directly reference the old property value.
        /// </summary>
        /// <param name="fieldSymbol">The input <see cref="IFieldSymbol"/> instance to process.</param>
        /// <param name="propertyName">The name of the property being generated.</param>
        /// <returns>Whether the generated code needs direct access to the old property value.</returns>
        //private static bool IsOldPropertyValueDirectlyReferenced(IFieldSymbol fieldSymbol, string propertyName)
        //{
        //    // Check On<PROPERTY_NAME>Changing(<PROPERTY_TYPE> oldValue, <PROPERTY_TYPE> newValue) first
        //    foreach (ISymbol symbol in fieldSymbol.ContainingType.GetMembers($"On{propertyName}Changing"))
        //    {
        //        // No need to be too specific as we're not expecting false positives (which also wouldn't really
        //        // cause any problems anyway, just produce slightly worse codegen). Just checking the number of
        //        // parameters is good enough, and keeps the code very simple and cheap to run.
        //        if (symbol is IMethodSymbol { Parameters.Length: 2 })
        //        {
        //            return true;
        //        }
        //    }

        //    // Do the same for On<PROPERTY_NAME>Changed(<PROPERTY_TYPE> oldValue, <PROPERTY_TYPE> newValue)
        //    foreach (ISymbol symbol in fieldSymbol.ContainingType.GetMembers($"On{propertyName}Changed"))
        //    {
        //        if (symbol is IMethodSymbol { Parameters.Length: 2 })
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// Gets the nullability info on the property
        /// </summary>
        /// <param name="propertySymbol">The input <see cref="IPropertySymbol"/> instance to process.</param>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> instance for the current run.</param>
        /// <param name="isReferenceTypeOrUnconstraindTypeParameter">Whether the property type supports nullability.</param>
        /// <param name="includeMemberNotNullOnSetAccessor">Whether <see cref="MemberNotNullAttribute"/> should be used on the setter.</param>
        /// <returns></returns>
        private static void GetNullabilityInfo(
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            out bool isReferenceTypeOrUnconstraindTypeParameter,
            out bool includeMemberNotNullOnSetAccessor)
        {
            // We're using IsValueType here and not IsReferenceType to also cover unconstrained type parameter cases.
            // This will cover both reference types as well T when the constraints are not struct or unmanaged.
            // If this is true, it means the field storage can potentially be in a null state (even if not annotated).
            isReferenceTypeOrUnconstraindTypeParameter = !propertySymbol.Type.IsValueType;

            // This is used to avoid nullability warnings when setting the property from a constructor, in case the field
            // was marked as not nullable. Nullability annotations are assumed to always be enabled to make the logic simpler.
            // Consider this example:
            //
            // [DTO]
            // partial class MyViewModel
            // {
            //    public MyViewModel()
            //    {
            //        Name = "Bob";
            //    }
            //
            //    [DTOMember]
            //    private string Name { get; set; }
            // }
            //
            // The [MemberNotNull] attribute is needed on the setter for the generated Name property so that when Name
            // is set, the compiler can determine that the name backing field is also being set (to a non null value).
            // Of course, this can only be the case if the field type is also of a type that could be in a null state.
            includeMemberNotNullOnSetAccessor =
                isReferenceTypeOrUnconstraindTypeParameter &&
                propertySymbol.Type.NullableAnnotation != NullableAnnotation.Annotated &&
                semanticModel.Compilation.HasAccessibleTypeWithMetadataName("System.Diagnostics.CodeAnalysis.MemberNotNullAttribute");
        }
    }
}

