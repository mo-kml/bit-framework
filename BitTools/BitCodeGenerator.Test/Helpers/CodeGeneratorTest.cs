﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.Helpers
{
    public class CodeGeneratorTest
    {
        public virtual Project CreateProjectFromSourceCodesWithExistingSolution(Solution existingSolution, params string[] sourceCodes)
        {
            ProjectId projectId = ProjectId.CreateNewId(debugName: "TestProjectName");

            existingSolution = existingSolution.AddProject(projectId, "TestProjectName", "TestProjectName", LanguageNames.CSharp);

            AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic)
                .ToList()
                .ForEach(asm =>
                {
                    existingSolution = existingSolution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(asm.Location));
                });

            for (int i = 0; i < sourceCodes.Length; i++)
            {
                DocumentId fileDocId = DocumentId.CreateNewId(projectId, debugName: $"File{i}.cs");

                existingSolution = existingSolution.AddDocument(fileDocId, $"File{i}.cs", SourceText.From(sourceCodes[i]));
            }

            return existingSolution.GetProject(projectId);
        }

        public virtual Project CreateProjectFromSourceCodes(params string[] sourceCodes)
        {
            Solution solution = new AdhocWorkspace()
                .CurrentSolution;

            return CreateProjectFromSourceCodesWithExistingSolution(solution, sourceCodes);
        }

        public virtual async Task<Workspace> GetWorkspace()
        {
            string solutionPath = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\Bit.sln");

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            await workspace.OpenSolutionAsync(solutionPath);

            return workspace;
        }
    }
}
