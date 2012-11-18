#region File Description
// -------------------------------------------------------------------------------------------------
// Copyright (C) 2012 Ed Ropple <ed+xnacontent@edropple.com>
//
// Derivative of works Copyright (C) Microsoft Corporation.
// Original project: http://xbox.create.msdn.com/en-US/education/catalog/sample/winforms_series_2
//
// Released under the Microsoft Public License: http://opensource.org/licenses/MS-PL
// -------------------------------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

#endregion

namespace EdCanHack.XnaContent
{
    /// <summary>
    /// Encapsulates a single content project and the building of the same.
    /// </summary>
    /// <remarks>
    /// This project implements Disposable, which will clean up the temporary
    /// directories when Dispose() is called. The finalizer for this class WILL NOT
    /// call Dispose(), so as to preserve the contents. You must manually dispose
    /// it to delete them.
    /// </remarks>
    public class ContentEngine : IDisposable
    {
        private static readonly String BaseDirectory = Path.Combine(Path.GetTempPath(), 
            Assembly.GetEntryAssembly().GetName().Name);
        private static readonly String ProcessDirectory = Path.Combine(BaseDirectory, Process.GetCurrentProcess().Id.ToString());
        private const String XnaVersion = ", Version=4.0.0.0, PublicKeyToken=842cf8be1de50553";

        private static Int32 _salt = 0;

        private static readonly String[] PipelineAssemblies =
        {
            "Microsoft.Xna.Framework.Content.Pipeline.FBXImporter" + XnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.XImporter" + XnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.TextureImporter" + XnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.EffectImporter" + XnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.AudioImporters" + XnaVersion,
            "Microsoft.Xna.Framework.Content.Pipeline.VideoImporters" + XnaVersion
        };

        // from: http://msdn.microsoft.com/en-us/library/bb447762.aspx
        private static readonly TypeMapping[] DefaultTypeMappings =
        {
            T(@".*\.fbw$", typeof(FbxImporter), typeof(ModelProcessor)),
            T(@".*\.x$", typeof(XImporter), typeof(ModelProcessor)),

            T(@".*\.fx$", typeof(EffectImporter), typeof(PassThroughProcessor)),

            T(@".*\.spritefont$", typeof(FontDescriptionImporter), typeof(FontDescriptionProcessor)),

            T(@".*\.(bmp|dds|dib|hdr|jpg|jpeg|pfm|png|ppm|tga)$", typeof(TextureImporter), typeof(TextureProcessor)),

            T(@".*\.wav$", typeof(WavImporter), typeof(SoundEffectProcessor)),
            T(@".*\.mp3$", typeof(Mp3Importer), typeof(SoundEffectProcessor)),
            T(@".*\.wma$", typeof(WmaImporter), typeof(SoundEffectProcessor)),

            T(@".*\.wmv$", typeof(WmvImporter), typeof(VideoProcessor))
        };


        private readonly List<String> _userAssemblies;
        private readonly List<TypeMapping> _typeMappings;
        private readonly Boolean _stripFileExtensions;
        private readonly Dictionary<String, QueuedContentFile> _files = 
            new Dictionary<String, QueuedContentFile>();

        private readonly String _overrideContentDirectory;
        private readonly Boolean _failOnUnmappedFiles;
        private readonly Boolean _useDefaultTypeMappings;

        public readonly String BuildDirectory;
        public String OutputDirectory { get { return Path.Combine(BuildDirectory, "bin/Content"); } }

        public Boolean IsDisposed { get; protected set; }

        public event ContentEngineItemAddedDelegate ItemAdded;
        public event ContentEngineItemRemovedDelegate ItemRemoved;
        public event ContentEngineStartingBuildDelegate BuildStarted;
        public event ContentEngineFinishedBuildDelegate BuildFinished;
        public event ContentEngineErrorDelegate BuildErrored;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userAssemblies">
        /// Any user assembles to add to the search path for the build project. If they are in the
        /// GAC, they must be strongly named; if they are not, they must have a file path.
        /// </param>
        /// <param name="typeMappings">
        /// A set of type mappings that supplement the standard XNA content types. These will be
        /// evaluated before the built-in types, such that overriding a .fbx or a .png handler will
        /// result in your handler, not the default one, being called.
        /// </param>
        /// <param name="contentDirectory">
        /// An optional parameter to specify the content directory for all content that will be
        /// added to this ContentEngine. This is only necessary if you are using ContentEngines
        /// without an XNA .contentproj file. If this is not set, then all generated content names
        /// (which will be used in your XNA games) will be determined by recursively searching your
        /// directory structure for the first .contentproj file found. For example, if your file
        /// is at D:/Project/Content/Textures/foo.png and the content project is at
        /// D:/Project/Content/Content.contentproj, then the computed content name will be
        /// Textures/foo[.png] (with the extension dependent upon the value of stripFileExtensions).
        /// </param>
        /// <param name="buildDirectory">
        /// An optional parameter to specify where the compiled XNBs should be stored. Defaults to
        /// a location in the user's temp directory.
        /// </param>
        /// <param name="stripFileExtensions">
        /// An optional parameter that determines whether or not the extension will be stripped
        /// from files added to the ContentEngine. The default value is true, which will emulate
        /// the behavior of the default XNA content pipeline projects within Visual Studio. If
        /// this parameter is false, then for content file "foo.png" the ContentEngine will
        /// generate an XNB file called "foo.png.xnb".
        /// </param>
        /// <param name="useDefaultTypeMappings">
        /// An optional parameter that determines whether the default type mappings (which map
        /// various file extensions to the standard XNA importers and processors) should be used
        /// to resolve filenames. Defaults to true.
        /// </param>
        /// <param name="failOnUnmappedFiles">
        /// An optional parameter that determines whether the ContentEngine should throw an exception
        /// if a file passed in via Add(String filename) has no matching TypeMapping objects in the
        /// search space.
        /// </param>
        public ContentEngine(IEnumerable<String> userAssemblies, IEnumerable<TypeMapping> typeMappings,
                             String contentDirectory = null, String buildDirectory = null, 
                             Boolean stripFileExtensions = true, Boolean useDefaultTypeMappings = true,
                             Boolean failOnUnmappedFiles = false)
        {
            _userAssemblies = new List<String>(userAssemblies);
            _typeMappings = new List<TypeMapping>(typeMappings);
            _stripFileExtensions = stripFileExtensions;
            _overrideContentDirectory = contentDirectory;
            _useDefaultTypeMappings = useDefaultTypeMappings;
            _failOnUnmappedFiles = failOnUnmappedFiles;

            BuildDirectory = buildDirectory ?? ComputeBuildDirectory();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(Boolean disposing)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            
            CleanUpSelf();
            CleanUpOldTempDirectories();
        }

        public IEnumerable<String> Filenames { get { return _files.Keys; } } 
        public IEnumerable<QueuedContentFile> Files { get { return _files.Values; } } 

        public void Add(String filename)
        {
            TypeMapping tm = null;
            tm = _typeMappings.FirstOrDefault(t => t.FileMatcher.IsMatch(filename));
                 
            if (_useDefaultTypeMappings && tm == null)
            {
                tm = DefaultTypeMappings.FirstOrDefault(t => t.FileMatcher.IsMatch(filename));
            }

            if (tm == null && _failOnUnmappedFiles)
            {
                throw new ContentEngineException("No TypeMapping found for '{0}'.", filename);
            }

            if (tm != null) Add(filename, tm.ImporterType, tm.ProcessorType);
        }
        public void Add(String filename, Type importerType, Type processorType)
        {
            filename = Path.GetFullPath(filename);

            if (!File.Exists(filename)) throw new FileNotFoundException(filename);

            String baseName;
            if (_overrideContentDirectory != null)
            {
                if (!filename.StartsWith(_overrideContentDirectory))
                {
                    throw new ContentEngineException("File '{0}' is outside of the defined " +
                                                     "content directory, '{1}'.", filename,
                                                     OutputDirectory);
                }

                baseName = filename.Substring(_overrideContentDirectory.Length + 1);
            }
            else
            {
                String directory = FindNearestContentProjectDirectory(filename);
                baseName = filename.Substring(directory.Length + 1);
            }

            if (_stripFileExtensions) baseName = Path.Combine(Path.GetDirectoryName(baseName),
                                                              Path.GetFileNameWithoutExtension(baseName));

            QueuedContentFile q = new QueuedContentFile(filename, baseName, importerType, processorType);

            _files.Add(q.Filename, q);

            if (ItemAdded != null) ItemAdded(q);
        }

        public Boolean Remove(String filename)
        {
            QueuedContentFile q;
            
            if (_files.TryGetValue(Path.GetFullPath(filename), out q))
            {
                if (ItemRemoved != null) ItemRemoved(q);
                _files.Remove(q.Filename);
            }

            return false;
        }

        public void Clear()
        {
            // this is so ItemRemoved gets fired for every removed file.
            List<String> filenames = new List<String>(_files.Keys);
            foreach (String f in filenames) Remove(f);
        }

        /// <summary>
        /// Performs a synchronous build of all content items that have been
        /// added to the content system.
        /// </summary>
        /// <returns>True if the build succeeded, false otherwise.</returns>
        public Boolean Build()
        {
            BuildSubmission build = null;
            try
            {
                if (!Directory.Exists(BuildDirectory)) Directory.CreateDirectory(BuildDirectory);

                String projectPath = Path.Combine(BuildDirectory, "Content.contentproj");
                String outputPath = Path.Combine(BuildDirectory, "bin");

                Project project = ConstructStandardProject(projectPath, outputPath);

                foreach (QueuedContentFile q in _files.Values)
                {
                    ProjectItem item = project.AddItem("Compile", q.Filename)[0];
                    // you'd think the item would need to be added to the project, but this API is weird.
                    // this just ends up mutating objects that the build system creates instead.

                    item.SetMetadataValue("Link", Path.GetFileName(q.Filename));
                    item.SetMetadataValue("Name", q.ContentName);

                    if (q.ImporterType != null) item.SetMetadataValue("Importer", q.ImporterType.Name);
                    if (q.ProcessorType != null) item.SetMetadataValue("Processor", q.ProcessorType.Name);
                }

                ErrorLogger logger = new ErrorLogger();
                BuildParameters parameters = new BuildParameters(ProjectCollection.GlobalProjectCollection)
                {
                    Loggers = new[] { logger }
                };

                if (BuildStarted != null) BuildStarted(project, parameters);

                if (File.Exists(projectPath)) File.Delete(projectPath);

                BuildManager.DefaultBuildManager.BeginBuild(parameters);

                BuildRequestData request = new BuildRequestData(project.CreateProjectInstance(), new String[0]);
                build = BuildManager.DefaultBuildManager.PendBuildRequest(request);

                build.ExecuteAsync(submission => {
                    if (submission.BuildResult.OverallResult == BuildResultCode.Success && BuildFinished != null)
                    {
                        BuildFinished(BuildDirectory, OutputDirectory, submission);
                    }
                    else if (submission.BuildResult.OverallResult == BuildResultCode.Failure && BuildErrored != null)
                    {
                        BuildErrored(submission, logger.Errors, null);
                    }
                }, null);

                build.WaitHandle.WaitOne();

                CleanUpOldTempDirectories();

                return build.BuildResult.OverallResult == BuildResultCode.Success;
            }
            catch (Exception ex)
            {
                if (BuildErrored != null) BuildErrored(build, new String[0], ex);
                return false;
            }
        }


        private Project ConstructStandardProject(String projectPath, String outputPath)
        {
            ProjectRootElement root = ProjectRootElement.Create(projectPath);
            root.AddImport(@"$(MSBuildExtensionsPath)\Microsoft\XNA Game Studio\" +
                           @"v4.0\Microsoft.Xna.GameStudio.ContentPipeline.targets");

            Project project = new Project(root);

            project.SetProperty("XnaPlatform", "Windows");
            project.SetProperty("XnaProfile", "Reach");
            project.SetProperty("XnaFrameworkVersion", "v4.0");
            project.SetProperty("Configuration", "Release");
            project.SetProperty("OutputPath", outputPath);

            foreach (String assembly in PipelineAssemblies)
            {
                project.AddItem("Reference", assembly);
            }
            String appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            foreach (String assembly in _userAssemblies)
            {
                String path = assembly;
                if (!Path.IsPathRooted(assembly))
                {
                    path = Path.Combine(appDirectory, assembly);
                }

                Assembly.LoadFile(path);
                project.AddItem("Reference", path);
            }

            return project;
        }


        /// <summary>
        /// Recursively deletes its build directory, the process directory if empty,
        /// and the base directory if empty, in that order.
        /// </summary>
        private void CleanUpSelf()
        {
            try
            {
                Directory.Delete(BuildDirectory, true);

                if (Directory.GetDirectories(ProcessDirectory).Length == 0)
                {
                    Directory.Delete(ProcessDirectory);

                    if (Directory.GetDirectories(BaseDirectory).Length == 0)
                    {
                        Directory.Delete(BaseDirectory);
                    }
                }
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
            // ReSharper restore EmptyGeneralCatchClause
            {
                // null exception, because if it fails, the next run will get it.
            }
        }


        private static String ComputeBuildDirectory()
        {
            return Path.Combine(ProcessDirectory, (_salt++).ToString());
        }
        private static TypeMapping T(String fileMatcherRegex, Type importerType, Type processorType)
        {
            return new TypeMapping(fileMatcherRegex, importerType, processorType);
        }
        private static String FindNearestContentProjectDirectory(String contentFilename)
        {
            String directory = Path.GetFullPath(Path.GetDirectoryName(contentFilename));
// ReSharper disable ConditionIsAlwaysTrueOrFalse
            while (directory != null) // R# says this is never null; it is wrong
// ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                String[] files = Directory.GetFiles(directory, "*.contentproj");
                if (files.Length > 0) return directory;

                directory = Path.GetFullPath(Path.GetDirectoryName(directory));
            }

            throw new ContentEngineException("Could not recursively locate a .contentproj file " +
                                             "for path '{0}'.", contentFilename);
        }

        private static void CleanUpOldTempDirectories()
        {
            if (!Directory.Exists(BaseDirectory)) return;

            foreach (String directory in Directory.GetDirectories(BaseDirectory))
            {
                // The subdirectory name is the ID of the process which created it.
                Int32 pid;

                if (Int32.TryParse(Path.GetFileName(directory), out pid))
                {
                    try
                    {
                        Process.GetProcessById(pid);
                    }
                    catch (ArgumentException)
                    {
                        // Exception is thrown if the process no longer exists.
                        try
                        {
                            Directory.Delete(directory, true);
                        }
// ReSharper disable EmptyGeneralCatchClause
                        catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
                        {
                            // this can fail for a number of mostly unimportant reasons, so I
                            // decided to just swallow it silently and go on.
                        }
                    }
                }
            }
        }
    }

    public delegate void ContentEngineItemAddedDelegate(QueuedContentFile file);
    public delegate void ContentEngineItemRemovedDelegate(QueuedContentFile file);
    public delegate void ContentEngineStartingBuildDelegate(Project project, BuildParameters parameters);
    public delegate void ContentEngineFinishedBuildDelegate(String buildDirectory, String contentDirectory, 
                                                            BuildSubmission build);
    public delegate void ContentEngineErrorDelegate(BuildSubmission build, IEnumerable<String> messages, Exception exception);
}
