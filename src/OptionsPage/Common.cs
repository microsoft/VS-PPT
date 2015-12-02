using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.PowerTools.OptionsPage
{
    internal static class Common
    {
        private static IOLEServiceProvider s_globalServiceProvider;

        /// <summary>
        /// Helper method to get a MefService from <see cref="GlobalServiceProvider"/>
        /// </summary>
        /// <typeparam name="T">The type of object to get</typeparam>
        /// <returns>The instance of the requested service</returns>
        internal static T GetMefService<T>() where T : class
        {
            IComponentModel componentModel = Common.GetComponentModel(Common.GlobalServiceProvider);
            if (componentModel != null)
                return componentModel.GetService<T>();

            return null;
        }

        /// <summary>
        /// GlobalServiceProvider for getting services from VisualStudio
        /// </summary>
        internal static IOLEServiceProvider GlobalServiceProvider
        {
            get
            {
                if (s_globalServiceProvider == null)
                {
                    s_globalServiceProvider = (IOLEServiceProvider)(Package.GetGlobalService(typeof(IOLEServiceProvider)));
                }

                return s_globalServiceProvider;
            }
        }

        private static IComponentModel GetComponentModel(IOLEServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            return (IComponentModel)(Common.GetService(serviceProvider, typeof(SComponentModel).GUID, unique: false));
        }

        private static object GetService(IOLEServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            Guid guidInterface = VSConstants.IID_IUnknown;
            IntPtr ptrObject = IntPtr.Zero;
            object service = null;

            int hr = serviceProvider.QueryService(ref guidService, ref guidInterface, out ptrObject);
            if (hr >= 0 && ptrObject != IntPtr.Zero)
            {
                try
                {
                    if (unique)
                    {
                        service = Marshal.GetUniqueObjectForIUnknown(ptrObject);
                    }
                    else
                    {
                        service = Marshal.GetObjectForIUnknown(ptrObject);
                    }
                }
                finally
                {
                    Marshal.Release(ptrObject);
                }
            }

            return service;
        }
    }
}
