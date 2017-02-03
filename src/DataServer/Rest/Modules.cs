// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;

namespace TrakHound.DataServer.Rest
{
    static class Modules
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private static List<IModule> _modules = new List<IModule>();
        public static ReadOnlyCollection<IModule> LoadedModules
        {
            get { return _modules.AsReadOnly(); }
        }

        public static void Load()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            // Get Modules embedded in the current assembly
            var modules = FindModules(Assembly.GetExecutingAssembly());
            if (modules != null)
            {
                foreach (var module in modules) log.Info("Module Loaded : " + module.Name);
            }

            _modules.AddRange(modules);
        }

        public static List<IModule> Get()
        {
            var l = new List<IModule>();

            foreach (var module in _modules)
            {
                l.Add((IModule)Activator.CreateInstance(module.GetType()));
            }

            return l;
        }

        public static IModule Get(Type t)
        {
            return (IModule)Activator.CreateInstance(t);
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
                if (Directory.Exists(dir))
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
                            log.Error(ex, "Module Initialization Error");
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
