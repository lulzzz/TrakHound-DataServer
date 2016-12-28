// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace TrakHound.DataServer.Data
{
    public class DataProcessor
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static string Get(string url, List<IModule> modules)
        {
            var uri = new Uri(url);
            return Get(uri, modules);
        }

        public static string Get(Uri uri, List<IModule> modules)
        {
            if (uri != null && modules != null && modules.Count > 0)
            {
                foreach (var module in modules)
                {
                    string response = module.GetResponse(uri);
                    if (!string.IsNullOrEmpty(response)) return response;
                }
            }

            return null;
        }

        public static List<IModule> LoadModules()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            // Get Modules embedded in the current assembly
            var modules = FindModules(Assembly.GetExecutingAssembly());
            if (modules != null)
            {
                foreach (var module in modules) log.Info("Module Loaded : " + module.Name);
            }

            return modules;
        }

        private class ModuleContainer
        {
            [ImportMany(typeof(IModule))]
            public IEnumerable<Lazy<IModule>> Modules { get; set; }
        }

        private static List<IModule> FindModules(string dir)
        {
            if (dir != null)
            {
                if (System.IO.Directory.Exists(dir))
                {
                    var catalog = new DirectoryCatalog(dir);
                    var container = new CompositionContainer(catalog);
                    return FindModules(container);
                }
            }

            return null;
        }

        private static List<IModule> FindModules(Assembly assembly)
        {
            if (assembly != null)
            {
                var catalog = new AssemblyCatalog(assembly);
                var container = new CompositionContainer(catalog);
                return FindModules(container);
            }

            return null;
        }

        private static List<IModule> FindModules(CompositionContainer container)
        {
            try
            {
                var moduleContainer = new ModuleContainer();
                container.SatisfyImportsOnce(moduleContainer);

                if (moduleContainer.Modules != null)
                {
                    var modules = new List<IModule>();

                    foreach (var lModule in moduleContainer.Modules)
                    {
                        try
                        {
                            var module = lModule.Value;
                            modules.Add(module);
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex, "Plugin Initialization Error");
                        }
                    }

                    return modules;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                log.Error(ex);

                foreach (var lex in ex.LoaderExceptions)
                {
                    log.Error(lex);
                }
            }
            catch (UnauthorizedAccessException ex) { log.Error(ex); }
            catch (Exception ex) { log.Error(ex); }

            return null;
        }

    }
}
