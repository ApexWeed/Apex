using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Apex.Win32;

namespace Apex.IO
{
    internal static class UnicodeFileSystemEnumerableFactory
    {
        internal static IEnumerable<string> CreateFileNameIterator(string Path, string SearchPattern, bool IncludeFiles, bool IncludeDirectories, SearchOption SearchOption)
        {
            var handler = new StringResultHandler(IncludeFiles, IncludeDirectories);
            return new UnicodeFileSystemEnumerableIterator<string>(Path, SearchPattern, SearchOption, handler);
        }

        internal static IEnumerable<UnicodeFileInfo> CreateFileInfoIterator(string Path, string SearchPattern, SearchOption SearchOption)
        {
            var handler = new FileInfoResultHandler();
            return new UnicodeFileSystemEnumerableIterator<UnicodeFileInfo>(Path, SearchPattern, SearchOption, handler);
        }

        internal static IEnumerable<UnicodeDirectoryInfo> CreateDirectoryInfoIterator(string Path, string SearchPattern, SearchOption SearchOption)
        {
            var handler = new DirectoryInfoResultHandler();
            return new UnicodeFileSystemEnumerableIterator<UnicodeDirectoryInfo>(Path, SearchPattern, SearchOption, handler);
        }

        internal static IEnumerable<UnicodeFileSystemInfo> CreateFileSystemInfoIterator(string Path, string SearchPattern, SearchOption SearchOption)
        {
            var handler = new FileSystemInfoResultHandler();
            return new UnicodeFileSystemEnumerableIterator<UnicodeFileSystemInfo>(Path, SearchPattern, SearchOption, handler);
        }
    }

    internal class UnicodeFileSystemEnumerableIterator<TSource> : Iterator<TSource>
    {
        private enum State
        {
            Init,
            SearchNextDir,
            FindNextFile,
            Finish
        }

        private SearchResultHandler<TSource> resultHandler;
        private List<SearchData> searchStack;
        private SearchData searchData;
        private string searchCritera;
        private SafeFindHandle handle;

        private bool empty;

        private string path;
        private string searchDir;
        private SearchOption searchOption;
        private int oldMode;

        new private State state;

        internal UnicodeFileSystemEnumerableIterator(string Path, string SearchPattern, SearchOption SearchOption, SearchResultHandler<TSource> ResultHandler)
        {
            SearchPattern = SearchPattern.Trim();
            if (SearchPattern == ".")
            {
                SearchPattern = "*";
            }

            UnicodePath.CheckSearchPattern(SearchPattern);

            oldMode = Win32Wrapper.SetErrorMode(Win32Wrapper.SEM_FAILCRITICALERRORS);
            searchStack = new List<SearchData>();

            if (SearchPattern.Length == 0)
            {
                empty = true;
            }
            else
            {
                resultHandler = ResultHandler;
                searchOption = SearchOption;

                path = UnicodePath.NormalisePath(Path, true);
                var fullSearchString = GetFullSearchString(path, SearchPattern);
                searchDir = UnicodePath.GetDirectoryName(fullSearchString);

                searchCritera = GetNormalisedSearchCriteria(fullSearchString, searchDir);

                searchData = new SearchData(searchDir, searchOption);

                CommonInit();
            }
        }

        private static string GetNormalisedSearchCriteria(string FullSearchString, string SearchDir)
        {
            var lastChar = SearchDir[SearchDir.Length - 1];
            if (UnicodePath.IsDirectorySeparator(lastChar))
            {
                return FullSearchString.Substring(SearchDir.Length);
            }
            else
            {
                return FullSearchString.Substring(SearchDir.Length + 1);
            }
        }

        private UnicodeFileSystemEnumerableIterator(string Path, string SearchCriteria, string SearchDir, SearchOption SearchOption, SearchResultHandler<TSource> ResultHandler)
        {
            path = Path;
            searchCritera = SearchCriteria;
            searchDir = SearchDir;
            searchOption = SearchOption;
            resultHandler = ResultHandler;

            searchStack = new List<SearchData>();

            if (searchCritera != null)
            {
                searchData = new SearchData(searchDir, searchOption);
                CommonInit();
            }
            else
            {
                empty = true;
            }
        }

        private void CommonInit()
        {
            var searchPath = Path.Combine(searchData.Path, searchCritera);
            var data = new Win32Wrapper.WIN32_FIND_DATA();
            handle = Win32Wrapper.FindFirstFile(searchPath, data);

            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();

                if (error != Win32Wrapper.ERROR_FILE_NOT_FOUND && error != Win32Wrapper.ERROR_NO_MORE_FILES)
                {
                    Throw(error, searchData.Path);
                }
                else
                {
                    empty = searchData.SearchOption == SearchOption.TopDirectoryOnly;
                }
            }

            if (searchData.SearchOption == SearchOption.TopDirectoryOnly)
            {
                // Dispose the handle if there's not contents.
                if (empty)
                {
                    handle.Dispose();
                }
                else
                {
                    var searchResult = CreateSearchResult(searchData, data);
                    if (resultHandler.IsResultIncluded(searchResult))
                    {
                        current = resultHandler.CreateObject(searchResult);
                    }
                }
            }
            else
            {
                handle.Dispose();
                searchStack.Add(searchData);
            }
        }

        public override bool MoveNext()
        {
            var data = new Win32Wrapper.WIN32_FIND_DATA();

            switch (state)
            {
                case State.Init:
                    {
                        if (empty)
                        {
                            state = State.Finish;
                            goto case State.Finish;
                        }
                        if (searchData.SearchOption == SearchOption.TopDirectoryOnly)
                        {
                            state = State.FindNextFile;
                            if (current != null)
                            {
                                return true;
                            }
                            else
                            {
                                goto case State.FindNextFile;
                            }
                        }
                        else
                        {
                            state = State.SearchNextDir;
                            goto case State.SearchNextDir;
                        }
                    }
                case State.SearchNextDir:
                    {
                        while (searchStack.Count > 0)
                        {
                            searchData = searchStack[0];
                            searchStack.RemoveAt(0);

                            AddSearchableDirsToStack(searchData);

                            var searchPath = Path.Combine(searchData.Path, searchCritera);

                            handle = Win32Wrapper.FindFirstFile(searchPath, data);
                            if (handle.IsInvalid)
                            {
                                var error = Marshal.GetLastWin32Error();
                                // Don't error on missing files.
                                if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND || error == Win32Wrapper.ERROR_NO_MORE_FILES || error == Win32Wrapper.ERROR_PATH_NOT_FOUND)
                                {
                                    continue;
                                }

                                handle.Dispose();
                                Throw(error, searchData.Path);
                            }

                            state = State.FindNextFile;
                            var searchResult = CreateSearchResult(searchData, data);
                            if (resultHandler.IsResultIncluded(searchResult))
                            {
                                current = resultHandler.CreateObject(searchResult);
                                return true;
                            }
                            else
                            {
                                goto case State.FindNextFile;
                            }
                        }

                        state = State.Finish;
                        goto case State.Finish;
                    }
                case State.FindNextFile:
                    {
                        if (searchData != null && handle != null)
                        {
                            while (Win32Wrapper.FindNextFile(handle, data))
                            {
                                var searchResult = CreateSearchResult(searchData, data);
                                if (resultHandler.IsResultIncluded(searchResult))
                                {
                                    current = resultHandler.CreateObject(searchResult);
                                    return true;
                                }
                            }

                            var error = Marshal.GetLastWin32Error();

                            if (handle != null)
                            {
                                handle.Dispose();
                            }

                            if (error != 0 && error != Win32Wrapper.ERROR_NO_MORE_FILES || error == Win32Wrapper.ERROR_FILE_NOT_FOUND)
                            {
                                Throw(error, searchData.Path);
                            }
                        }

                        if (searchData.SearchOption == SearchOption.TopDirectoryOnly)
                        {
                            state = State.Finish;
                            goto case State.Finish;
                        }
                        else
                        {
                            state = State.SearchNextDir;
                            goto case State.SearchNextDir;
                        }
                    }
                case State.Finish:
                    {
                        Dispose();
                        break;
                    }
            }
            return false;
        }

        protected override Iterator<TSource> Clone()
        {
            return new UnicodeFileSystemEnumerableIterator<TSource>(path, searchCritera, searchDir, searchOption, resultHandler);
        }

        private static string GetFullSearchString(string Path, string SearchPattern)
        {
            var fullPath = System.IO.Path.Combine(Path, SearchPattern);

            if (UnicodePath.IsDirectorySeparator(fullPath[fullPath.Length - 1]) || fullPath[fullPath.Length - 1] == System.IO.Path.VolumeSeparatorChar)
            {
                fullPath = fullPath + '*';
            }

            return fullPath;
        }

        private void AddSearchableDirsToStack(SearchData LocalSearchData)
        {
            var searchPath = Path.Combine(LocalSearchData.Path, "*");
            SafeFindHandle localHandle = null;
            var data = new Win32Wrapper.WIN32_FIND_DATA();

            try
            {
                localHandle = Win32Wrapper.FindFirstFile(searchPath, data);

                if (localHandle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error();

                    if (error == Win32Wrapper.ERROR_FILE_NOT_FOUND || error == Win32Wrapper.ERROR_NO_MORE_FILES || error == Win32Wrapper.ERROR_PATH_NOT_FOUND)
                    {
                        return;
                    }

                    Throw(error, searchData.Path);
                }

                var pos = 0;
                do
                {
                    if (FileSystemEnumerableHelpers.IsDirectory(data))
                    {
                        var localPath = Path.Combine(LocalSearchData.Path, data.cFileName);
                        var option = LocalSearchData.SearchOption;

                        // Don't search symbolic links and what not.
                        if (LocalSearchData.SearchOption == SearchOption.AllDirectories && (data.dwFileAttributes & Win32Wrapper.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                        {
                            option = SearchOption.TopDirectoryOnly;
                        }

                        var searchDataSubDir = new SearchData(localPath, option);

                        searchStack.Insert(pos++, searchDataSubDir);
                    }
                } while (Win32Wrapper.FindNextFile(localHandle, data));

                // We are reckless and don't mind some errors being ignored.
            }
            finally
            {
                if (localHandle != null)
                {
                    localHandle.Dispose();
                }
            }
        }

        private void Throw(int Error, string Path)
        {
            Dispose();
            __Error.WinIOError(Error, Path);
        }

        private static SearchResult CreateSearchResult(SearchData LocalSearchData, Win32Wrapper.WIN32_FIND_DATA Data)
        {
            return new SearchResult(Path.Combine(LocalSearchData.Path, Data.cFileName), Data);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (handle != null)
                {
                    handle.Dispose();
                }
            }
            finally
            {
                Win32Wrapper.SetErrorMode(oldMode);
                base.Dispose(disposing);
            }
        }
    }

    internal sealed class SearchResult
    {
        internal string Path { get; private set; }
        internal Win32Wrapper.WIN32_FIND_DATA Data { get; private set; }

        internal SearchResult(string Path, Win32Wrapper.WIN32_FIND_DATA Data)
        {
            this.Path = Path;
            this.Data = Data;
        }
    }

    internal abstract class SearchResultHandler<TSource>
    {
        internal abstract bool IsResultIncluded(SearchResult Result);
        internal abstract TSource CreateObject(SearchResult Result);
    }

    internal class StringResultHandler : SearchResultHandler<string>
    {
        private bool includeFiles;
        private bool includeDirectories;

        internal StringResultHandler(bool IncludeFiles, bool IncludeDirectories)
        {
            includeFiles = IncludeFiles;
            includeDirectories = IncludeDirectories;
        }

        internal override string CreateObject(SearchResult Result)
        {
            return Result.Path;
        }

        internal override bool IsResultIncluded(SearchResult Result)
        {
            var filePass = includeFiles && FileSystemEnumerableHelpers.IsFile(Result.Data);
            var directoryPass = includeDirectories && FileSystemEnumerableHelpers.IsDirectory(Result.Data);

            return (filePass || directoryPass);
        }
    }

    internal class FileInfoResultHandler : SearchResultHandler<UnicodeFileInfo>
    {
        internal override UnicodeFileInfo CreateObject(SearchResult Result)
        {
            var fileInfo = new UnicodeFileInfo(Result.Path);
            fileInfo.InitialiseFrom(Result.Data);
            return fileInfo;
        }

        internal override bool IsResultIncluded(SearchResult Result)
        {
            return FileSystemEnumerableHelpers.IsFile(Result.Data);
        }
    }

    internal class DirectoryInfoResultHandler : SearchResultHandler<UnicodeDirectoryInfo>
    {
        internal override UnicodeDirectoryInfo CreateObject(SearchResult Result)
        {
            var directoryInfo = new UnicodeDirectoryInfo(Result.Path);
            directoryInfo.InitialiseFrom(Result.Data);
            return directoryInfo;
        }

        internal override bool IsResultIncluded(SearchResult Result)
        {
            return FileSystemEnumerableHelpers.IsDirectory(Result.Data);
        }
    }

    internal class FileSystemInfoResultHandler : SearchResultHandler<UnicodeFileSystemInfo>
    {
        internal override UnicodeFileSystemInfo CreateObject(SearchResult Result)
        {
            var isFile = FileSystemEnumerableHelpers.IsFile(Result.Data);

            if (isFile)
            {
                var fileInfo = new UnicodeFileInfo(Result.Path);
                fileInfo.InitialiseFrom(Result.Data);
                return fileInfo;
            }
            else
            {
                var directoryInfo = new UnicodeDirectoryInfo(Result.Path);
                directoryInfo.InitialiseFrom(Result.Data);
                return directoryInfo;
            }
        }

        internal override bool IsResultIncluded(SearchResult Result)
        {
            var filePass = FileSystemEnumerableHelpers.IsFile(Result.Data);
            var directoryPass = FileSystemEnumerableHelpers.IsDirectory(Result.Data);

            return (filePass || directoryPass);
        }
    }

    internal static class FileSystemEnumerableHelpers
    {
        internal static bool IsDirectory(Win32Wrapper.WIN32_FIND_DATA Data)
        {
            return ((Data.dwFileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) != 0 && !Data.cFileName.Equals(".") && !Data.cFileName.Equals(".."));
        }

        internal static bool IsFile(Win32Wrapper.WIN32_FIND_DATA Data)
        {
            return (Data.dwFileAttributes & Win32Wrapper.FILE_ATTRIBUTE_DIRECTORY) == 0;
        }
    }

    internal class SearchData
    {
        internal string Path { get; private set; }
        internal SearchOption SearchOption { get; private set; }

        internal SearchData(string Path, SearchOption SearchOption)
        {
            this.Path = Path;
            this.SearchOption = SearchOption;
        }
    }
}
