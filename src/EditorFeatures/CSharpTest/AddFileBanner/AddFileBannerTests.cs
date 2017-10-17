﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddFileBanner;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddFileBanner
{
    public partial class AddFileBannerTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpAddFileBannerCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
        public async Task TestBanner1()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// This is the banner

class Program2
{
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>// This is the banner

using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// This is the banner

class Program2
{
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
        public async Task TestMultiLineBanner1()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// This is the banner
// It goes over multiple lines

class Program2
{
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>// This is the banner
// It goes over multiple lines

using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// This is the banner
// It goes over multiple lines

class Program2
{
}
        </Document>
    </Project>
</Workspace>");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
        public async Task TestMissingWhenAlreadyThere()
        {
            await TestMissingAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]// I already have a banner

using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// This is the banner

class Program2
{
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
        public async Task TestMissingIfOtherFileDoesNotHaveBanner()
        {
            await TestMissingAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]

using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>

class Program2
{
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
        public async Task TestMissingIfOtherFileIsAutoGenerated()
        {
            await TestMissingAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>[||]

using System;

class Program1
{
    static void Main()
    {
    }
}
        </Document>
        <Document>// &lt;autogenerated /&gt;

class Program2
{
}
        </Document>
    </Project>
</Workspace>");
        }
    }
}