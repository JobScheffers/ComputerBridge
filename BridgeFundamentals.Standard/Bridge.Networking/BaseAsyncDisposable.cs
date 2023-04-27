using System;
using System.Threading.Tasks;

namespace Bridge.NonBridgeHelpers
{
//#if NET6_0_OR_GREATER
    public abstract class BaseAsyncDisposable : IAsyncDisposable
    {
        protected bool IsDisposed = false; // To detect redundant calls

        protected abstract ValueTask DisposeManagedObjects();

        // This code added to correctly implement the disposable pattern.
        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in DisposeManagedObjects above.
            await this.DisposeManagedObjects();
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }
    }
//#else
//    public abstract class BaseAsyncDisposable : IAsyncDisposable
//    {
//        protected bool IsDisposed = false; // To detect redundant calls

//        protected abstract ValueTask DisposeManagedObjects();

//        // This code added to correctly implement the disposable pattern.
//        public async ValueTask DisposeAsync()
//        {
//            // Do not change this code. Put cleanup code in DisposeManagedObjects above.
//            GC.SuppressFinalize(this);
//            IsDisposed = true;
//            await Task.CompletedTask;
//        }
//    }
//#endif
}
