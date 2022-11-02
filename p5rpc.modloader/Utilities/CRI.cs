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
    /// Get allocation size needed for work directory.
    /// </summary>
    /// <param name="srcbndrhn">Binder handle to access directory to bind.</param>
    /// <param name="path">Path to the folder to bind.</param>
    /// <param name="workSize">Necessary work size.</param>
    /// <returns>CriError Error code.</returns>
    [Function(CallingConventions.Microsoft)]
    public delegate CriError criFsBinder_GetWorkSizeForBindDirectory(IntPtr srcbndrhn, [MarshalAs(UnmanagedType.LPStr)] string path, int* workSize);
    
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

    /// <summary>
    /// Specifies the configuration of the CRI File System Library.
    /// This structure is specified as an argument when the library is initialised (i.e. criFs_InitializeLibrary).
    /// </summary>
    /// <remarks>
    /// This function specifies the amount of memory that will be preallocated ahead of time for internal resources.
    /// If a value specified is too small, allocation/binding might fail. 
    /// </remarks>
    public struct CriFsConfig
    {
	    /// <summary>
	    /// Specifies the thread model of the filesystem.
	    /// </summary>
	    public CriFsThreadModel ThreadModel;
	    
	    /// <summary>
		/// Number of CriFsBinder binders to be used.
		///
		/// Specify the number of binders (CriFsBinder) you want to use in the application. <br/>
		/// When creating a binder using the criFsBinder_Create function in the application,
		/// you must specify the number of binders to use in this parameter. <br/> <br/>
		///
		/// Specify the "maximum number of binders to use simultaneously" for num_binders. <br/>
		/// For example, in the case where the criFsBinder_Create and criFsBinder_Destroy functions are executed alternately and continuously,
		/// the maximum simultaneous number of binder to be used is one; this lets you specify 1 for um_binders regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 binders are used, even if no binder is used in other situations,
		/// you must specify 10 for num_binders. <br/>
		///
		/// At the time of initialization, the CRI File System library makes a request for memory allocation for the number of binders to be used. <br/>
		/// \sa criFsBinder_Create, criFsBinder_Destroy
		/// </summary>
	    public int NumBinders;

		/// <summary>
		/// Number of CriFsLoader loaders to use
		///
		/// Specify the number of loaders (CriFsLoader) you want to use in the application. <br/>
		/// When creating a loader using the criFsLoader_Create function in the application,
		/// you must specify the number of loaders to use in this parameter. <br/>
		///
		/// <br/>
		/// Specify the "maximum number of loaders to use simultaneously" for num_loaders. <br/>
		/// For example, in the case where the criFsLoader_Create and criFsLoader_Destroy functions are executed alternately and continuously,
		/// the maximum simultaneous number of loader to be used is one; this lets you specify 1 for um_loaders regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 loaders are used, even if no loader is used in other situations,
		/// you must specify 10 for num_loaders. <br/>
		///
		/// At the time of initialization, the CRI File System library makes a request for memory allocation for the number of loaders to be used. <br/>
		/// This reduces the memory size required by the library by setting num_loaders to the minimum necessary value. <br/>
		/// 
		/// </summary>
		public int NumLoaders;

		/// <summary>
		/// Number of CriFsGroupLoader loaders to be used
		///
		/// Specify the number of group loaders (CriFsGroupLoader) you want to use in the application. <br/>
		/// When creating a group loader using the criFsGroupLoader_Create function in the application,
		/// you must specify the number of group loaders to use in this parameter. <br/>
		/// <br/>
		/// 
		/// Specify the "maximum number of group loaders to use simultaneously" for num_group_loaders. <br/>
		/// For example, in the case where the criFsGoupLoader_Create and criFsGroupLoader_Destroy functions are executed alternately and continuously,
		/// the maximum simultaneous number of group loaders to be used is one; this lets you specify 1 for num_group_loaders regardless how many times the functions are called. <br/>
		///
		/// On the other hand, for a case where 10 group loaders are used, even if no group loader is used in other situations,
		/// you must specify 10 for num_group_loaders. <br/>
		///
		/// At the time of initialization, the CRI File System library makes a request for memory allocation for the number of group loaders to be used. <br/>
		/// This reduces the memory size required by the library by setting num_group_loaders to the minimum necessary value. <br/>
		/// </summary>
		public int NumGroupLoaders;

		/// <summary>
		/// Number of CriFsStdio handles to use.
		///
		/// This function specifies the number of CriFsStdio handles you want to use in the application. <br/>
		/// When creating a CriFsStdio handle using the criFsStdio_OpenFile function in the application,
		/// you must specify the number of CriFsStdio handles to use in this parameter. <br/>
		///
		/// Specify the "maximum number of CriFsStdio handles to use simultaneously" for num_stdio_handles. <br/><br/>
		///
		/// For example, in the case where the :criFsStdio_OpenFile and criFsStdio_CloseFile functions are executed alternately and continuously,
		/// the maximum simultaneous number of CriFsStdio handles to be used is one; this lets you specify 1 for num_stdio_handles regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 CriFsStdio handles are used, even if no CriFsStdio handle is used in other situations,
		/// you must specify 10 for num_stdio_handles. <br/>
		/// At the time of initialization, the CRI File System library makes a request for memory allocation for the number of CriFsStdio handles to be used. <br/>
		/// This reduces the memory size required by the library by setting num_stdio_handles to the minimum necessary value. <br/>
		/// </summary>
		public int NumStdioHandles;

		/// <summary>
		/// Number of CriFsInstaller installers to use.
		///
		/// Specify the number of installers (CriFsInstaller) you want to use in the application. <br/>
		/// When creating a CriFsInstaller installer using the criFsInstaller_Create function in the application,
		/// you must specify the number of the installers to use in this parameter. <br/>
		///
		/// <br/>
		/// Specify the "maximum number of installers to use simultaneously" for num_installers. <br/>
		/// For example, in the case where the criFsInstaller_Create and criFsInstaller_Destroy functions are executed alternately and continuously,
		/// the maximum simultaneous number of the installers to be used is one; this lets you specify 1 for num_installers regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 of the installers are used, even if this installer is not used in other situations,
		/// you must specify 10 for num_installers. <br/>
		/// </summary>
		public int NumInstallers;
		
	
		/// <summary>
		/// Maximum simultaneous number of bind processes
		///
		/// Perform the bind processing in the application and specify the number of bind IDs (CriFsBindId) to retain. <br/>
		/// When performing bind processing using the ::criBinder_BindCpk function in the application,
		/// you must specify the number of bind IDs to use in this parameter. <br/>
		/// <br/>
		/// Specify the "maximum number of bind IDs to use simultaneously" for max_binds. <br/>
		/// For example, in the case where the criFsBinder_BindCpk and criFsBinder_Unbind functions are executed alternately and continuously,
		/// the maximum simultaneous number of bind IDs to be used is one; this lets you specify 1 for max_binds regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 bind IDs are used, even if no bind is used in other situations,
		/// you must specify 10 for max_binds. <br/>
		/// At the time of initialization, the CRI File System library makes a request for memory allocation for the number of bind IDs to be used. <br/>
		/// This reduces the memory size required by the library by setting max_binds to the minimum necessary value. <br/>
		/// </summary>
		public int MaxBinds;

		/// <summary>
		/// Maximum simultaneous number of files to open.
		///
		/// Specify the number of files you want to open in the application. <br/>
		/// When opening a file using the criFsStdio_OpenFile or other functions in the application,
		/// you must specify the number of files to open in this parameter. <br/> <br/>
		///
		/// Specify the "maximum number of files to open simultaneously" for max_files. <br/>
		/// For example, in the case where the :criFsStdio_OpenFile and criFsStdio_CloseFile functions are executed alternately and continuously,
		/// the maximum simultaneous number files to be opened is one; this lets you specify 1 for max_files regardless how many times the functions are called. <br/>
		/// On the other hand, for a case where 10 files are opened, even if only one file is opened in other situations,
		/// you must specify 10 for max_files. <br/>
		///
		/// Additional information:
		/// The CRI File System library opens a file when executing the following functions. <br/>
		/// Cases where a file is opened" align=center border=1 cellspacing=0 cellpadding=4
		///
		/// [criFsBinder_BindCpk	|One file is opened. <br/> Until the criFsBinder_Unbind function is executed, the file is kept open. 	]
		/// [criFsBinder_BindFile	|One file is opened. <br/> Until the criFsBinder_Unbind function is executed, the file is kept open. 	]
		/// [criFsBinder_BindFiles	|Files for the number included in the list are opened. <br/> Until the criFsBinder_Unbind function is executed, the files are kept open.]
		/// [criFsLoader_Load	|One file is opened. <br/> Until the load is completed, the file is kept open. <br/> With a binder specified, no file is opened (Because the binder has already opened a file). 	]
		/// [criFsStdio_OpenFile	|One file is opened. <br/> Until the criFsStdio_CloseFile function is executed, the file is kept open. <br/> With a binder specified, no file is opened (Because the binder has already opened a file). 	]
		/// [criFsInstaller_Copy	|Two files are opened. <br/> Until the file copy is completed, the files are kept open. <br/> With a binder specified, one file will be opened (Because the binder has already opened the other file). 	]
		///
		/// When using the ADX library together with the CRI Vibe library or other libraries using the bridge library,
		/// The ADXT or criSsPly handle internally creates the CriFsStdio handle. <br/>
		///
		/// Therefore, to use the bridge library, specify for max_files the number of the CriFsStdio handles plus the number of ADXT or criSsPly handles
		/// when initializing the CRI File System library. <br/>
		/// </summary>
		public int MaxFiles;

		/// <summary>
		/// Maximum length of the path (in bytes)
		/// 
		/// Specify the maximum length of the file path you want to specify in the application. <br/>
		/// When accessing a file using the criFsLoader_Load or other functions in the application,
		/// you must specify in this parameter the maximum length of a path string you want to use in the application. <br/>
		///
		/// Specify the "maximum length of a path string to use" for max_path. <br/>
		/// For a case where a 256-byte file path is used, you must specify 256 for max_path
		/// even if only a 32-byte file path is used in other cases. <br/>
		///
		/// For the maximum length of a path, you must specify a value that includes the number of NULL characters located at the end. <br/>
		/// (The value of "the number of characters + 1 byte" must be specified.) <br/>
		///
		/// Note that when a user can install an application in a desired location such as a PC, the assumed maximum size must be specified in the max_path. <br/>
		/// </summary>
		public int MaxPath;

		/// <summary>
		/// Library version number
		/// 
		/// This is the version number of the CRI File System library. <br/>
		/// The version number defined in this header is set by the criFs_SetDefaultConfig function. <br/>
		/// </summary>
		public int Version;

		/// <summary>
		/// This flag is used to switch whether to perform data integrity check using CRC information in the CPK file. <br/>
		/// When this flag is set to CRI_TRUE (1), CRC check is performed at the following timing.
		///
		/// - CRC check of TOC information at CPK bind
		/// - CRC check in content file unit when loading content file
		///
		/// An error will occur if the CRC information attached to the CPK does not match the CRC of the actually read data.
		/// </summary>
		public int EnableCrcCheck;
    }
    
 	/// <summary>
 	/// Specifies the CRI FileSystem model under which the thread operates.
 	/// This is specified in <see cref="CriFsConfig"/> structure.
 	/// </summary>
	public enum CriFsThreadModel : int
    {
		/// <summary>
		/// Library creates threads and operates multithreaded.
		/// A background thread is spawned when library is initialised. (criFs_InitializeLibrary)
		/// </summary>
		CRIFS_THREAD_MODEL_MULTI = 0,

		/// <summary>
		/// The library creates threads and operates multithreaded.
		/// A background thread is spawned when library is initialised. (criFs_InitializeLibrary)
		///
		/// The server processing is executed on the created thread; but not automatically executed unlike in <see cref="CRIFS_THREAD_MODEL_MULTI"/>.
		/// 
		/// The user must explicitly execute the processing on the server using the (criFs_ExecuteMain) function.
		/// (Executing criFs_ExecuteMain function starts the thread to execute the processing on the server.) <br/>
		/// </summary>
		CRIFS_THREAD_MODEL_MULTI_USER_DRIVEN = 3,

		/// <summary>
		/// No thread is created but exclusion control is performed inside the library for the server processing
		/// functions (criFs_ExecuteFileAccess, criFs_ExecuteDataDecompression) to be able to be called from a user-created thread.
		/// </summary>
		CRIFS_THREAD_MODEL_USER_MULTI = 1,

		/// <summary>
		/// No thread is created inside the library. Exclusion control is not performed inside the library either.
		/// When selecting this model, call the APIs and server processing functions (criFs_ExecuteFileAccess, criFs_ExecuteDataDecompression) from the same thread.
		/// </summary>
		CRIFS_THREAD_MODEL_SINGLE = 2
    }
}