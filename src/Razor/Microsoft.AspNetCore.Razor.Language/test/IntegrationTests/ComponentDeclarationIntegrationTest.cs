// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentDeclarationRazorIntegrationTest : RazorIntegrationTestBase
    {
        public ComponentDeclarationRazorIntegrationTest()
        {
            // Include this assembly to use types defined in tests.
            BaseCompilation = DefaultBaseCompilation.AddReferences(MetadataReference.CreateFromFile(GetType().Assembly.Location));
        }

        internal override CSharpCompilation BaseCompilation { get; }

        internal override string FileKind => FileKinds.Component;

        internal override bool DeclarationOnly => true;

        [Fact]
        public void DeclarationConfiguration_IncludesFunctions()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@functions {
    public string Value { get; set; }
}");

            // Assert
            var property = component.GetType().GetProperty("Value");
            Assert.NotNull(property);
            Assert.Same(typeof(string), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesInject()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@inject string Value
");

            // Assert
            var property = component.GetType().GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Same(typeof(string), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesUsings()
        {
            // Arrange & Act
            var component = CompileToComponent(@"
@using System.Text
@inject StringBuilder Value
");

            // Assert
            var property = component.GetType().GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(property);
            Assert.Same(typeof(StringBuilder), property.PropertyType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesInherits()
        {
            // Arrange & Act
            var component = CompileToComponent($@"
@inherits {FullTypeName<BaseClass>()}
");

            // Assert
            Assert.Same(typeof(BaseClass), component.GetType().BaseType);
        }

        [Fact]
        public void DeclarationConfiguration_IncludesImplements()
        {
            // Arrange & Act
            var component = CompileToComponent($@"
@implements {FullTypeName<IDoCoolThings>()}
");

            // Assert
            var type = component.GetType();
            Assert.Contains(typeof(IDoCoolThings), component.GetType().GetInterfaces());
        }

        [Fact] // Regression test for https://github.com/dotnet/blazor/issues/453
        public void DeclarationConfiguration_FunctionsBlockHasLineMappings_MappingsApplyToError()
        {
            // Arrange & Act 1
            var generated = CompileToCSharp(@"
@functions {
    public StringBuilder Builder { get; set; }
}
");

            // Assert 1
            AssertSourceEquals(@"
// <auto-generated/>
#pragma warning disable 1591
#pragma warning disable 0414
#pragma warning disable 0649
#pragma warning disable 0169

namespace Test
{
    #line hidden
    #pragma warning disable 8019
    using System;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Collections.Generic;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Linq;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using System.Threading.Tasks;
    #pragma warning restore 8019
    #pragma warning disable 8019
    using Microsoft.AspNetCore.Components;
    #pragma warning restore 8019
    public partial class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
        }
        #pragma warning restore 1998
#nullable restore
#line 1 ""x:\dir\subdir\Test\TestComponent.cshtml""
            
    public StringBuilder Builder { get; set; }

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591
", generated);

            // Act 2
            var assembly = CompileToAssembly(generated, throwOnFailure: false);

            // Assert 2
            var diagnostic = Assert.Single(assembly.Diagnostics);

            // This error should map to line 2 of the generated file, the test
            // says 1 because Roslyn's line/column data structures are 0-based.
            var position = diagnostic.Location.GetMappedLineSpan();
            Assert.EndsWith(".cshtml", position.Path);
            Assert.Equal(1, position.StartLinePosition.Line);
        }

        public class BaseClass : IComponent
        {
            public void Attach(RenderHandle renderHandle)
            {
            }

            protected virtual void BuildRenderTree(RenderTreeBuilder builder)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                throw new System.NotImplementedException();
            }
        }

        public interface IDoCoolThings
        {
        }
    }
}
