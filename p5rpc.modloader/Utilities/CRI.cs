using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions.X64;

namespace p5rpc.modloader.Utilities;

// Documentation taken from publicly available CRI SDK
public static unsafe class CRI
{
    /// <summary>
    /// Generate a binder.
    /// This function generates a binder and returns a binder handle. 
    /// </summary>
    /// <param name="bndrhn">[out] Binder handle.</param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_Create(IntPtr* bndrhn);

    /// <summary>
    /// Get the bind status. 
    /// </summary>
    /// <param name="bndrid">[in] Bind ID.</param>
    /// <param name="status">[out] Binder status. </param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_GetStatus(uint bndrid, CriFsBinderStatus* status);
    
    /// <summary>
    /// Bind the CPK file. 
    /// </summary>
    /// <param name="bndrhn">Binder handle of the bind destination.</param>
    /// <param name="srcbndrhn">Binder handle to access the CPK file to bind.</param>
    /// <param name="path">Path name of the CPK file to bind.</param>
    /// <param name="work">Work area for bind (mainly for CPK analysis).</param>
    /// <param name="worksize">Size of the work area (bytes).</param>
    /// <param name="bndrid">[out] Bind ID.</param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_BindCpk(IntPtr bndrhn, IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, IntPtr work, int worksize, uint* bndrid);
    
    /// <summary>
    /// This function sets the priority value for the bind ID. 
    /// Using the priority enables you to control the order of searching the bind IDs in a binder handle. 
    /// The priority value of the ID is 0 when bound, and IDs are searched in the binding order of them with the same priority. 
    /// The larger the priority value is, the higher the priority with higher search order is. 
    /// </summary>
    /// <param name="bndrid">Bind ID.</param>
    /// <param name="priority">Priority value.</param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_SetPriority(uint bndrid, int priority);
    
    /// <summary>
    /// Delete bind ID (Unbind): Blocking function. 
    /// </summary>
    /// <param name="bndrid">Bind ID.</param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_Unbind(uint bndrid);
    
    /// <summary>
    /// Status of the CRI binder.
    /// </summary>
    public enum CriFsBinderStatus : int
    {
        CRIFSBINDER_STATUS_NONE = 0,
        
        /// <summary>Binding.</summary>
        CRIFSBINDER_STATUS_ANALYZE,
        
        /// <summary>Bound.</summary>
        CRIFSBINDER_STATUS_COMPLETE,
        
        /// <summary>Unbinding.</summary>
        CRIFSBINDER_STATUS_UNBIND,
        
        /// <summary>Unbound.</summary>
        CRIFSBINDER_STATUS_REMOVED,
        
        /// <summary>Invalid Bind.</summary>
        CRIFSBINDER_STATUS_INVALID,
        
        /// <summary>Bind Failed.</summary>
        CRIFSBINDER_STATUS_ERROR
    }

    public enum CriError : int
    {
        /// <summary>Succeeded.</summary>
        CRIERR_OK = 0,
        
        /// <summary>Error occurred.</summary>
        CRIERR_NG = -1,
        
        /// <summary>Invalid argument.</summary>
        CRIERR_INVALID_PARAMETER = -2,
        
        /// <summary>Failed to allocate memory.</summary>
        CRIERR_FAILED_TO_ALLOCATE_MEMORY = -3,
        
        /// <summary>Parallel execution of thread-unsafe function.</summary>
        CRIERR_UNSAFE_FUNCTION_CALL = -4,
        
        /// <summary>Function not implemented.</summary>
        CRIERR_FUNCTION_NOT_IMPLEMENTED = -5,
        
        /// <summary>Library not initialized.</summary>
        CRIERR_LIBRARY_NOT_INITIALIZED = -6,
    }
}