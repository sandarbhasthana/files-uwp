﻿using System;
using Files;
using Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Search;
using Windows.UI.Popups;
using Interacts;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using System.Threading;
using System.Threading.Tasks;

namespace ItemListPresenter
{
    public class ListedItem
    {
        public Visibility FolderImg { get; set; }
        public Visibility FileIconVis { get; set; }
        public BitmapImage FileImg { get; set; }
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public int ItemIndex { get; set; }
        public ListedItem()
        {

        }
    }

    public class Classic_ListedFolderItem
    {
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public ObservableCollection<Classic_ListedFolderItem> Children { get; set; } = new ObservableCollection<Classic_ListedFolderItem>();
    }

    public class ItemViewModel
    {
        public static ObservableCollection<Classic_ListedFolderItem> classicFolderList = new ObservableCollection<Classic_ListedFolderItem>();
        public static ObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get { return classicFolderList; } }
        
        public static ObservableCollection<ListedItem> classicFileList = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> ClassicFileList { get { return classicFileList; } }

        public static ObservableCollection<ListedItem> filesAndFolders = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> FilesAndFolders { get { return filesAndFolders; } }

        StorageFolder folder;
        static string gotName;
        static string gotDate;
        static string gotType;
        static string gotPath;
        static string gotFolName;
        static string gotFolDate;
        static string gotFolPath;
        static string gotFolType;
        static Visibility gotFileImgVis;
        static Visibility gotFolImg;
        static StorageItemThumbnail gotFileImg;
        public static ObservableCollection<Classic_ListedFolderItem> ChildrenList;
        public static IReadOnlyList<StorageFolder> folderList;
        public static IReadOnlyList<StorageFile> fileList;
        public static bool isPhotoAlbumMode;
        public static string pageName;

        public static ItemViewModel vm;
        public static ItemViewModel ViewModel { get { return vm; } set { } }

        public static BackState bs = new BackState();
        public static BackState BS
        {
            get
            {
                return bs;
            }
            set
            {

            }
        }

        public static ForwardState fs = new ForwardState();
        public static ForwardState FS
        {
            get
            {
                return fs;
            }
            set
            {

            }
        }

        public static ProgressUIVisibility pvis = new ProgressUIVisibility();
        public static ProgressUIVisibility PVIS
        {
            get
            {
                return pvis;
            }
            set
            {

            }
        }

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return this.li; } }

        private static ProgressUIHeader pUIh = new ProgressUIHeader();
        public static ProgressUIHeader PUIH { get { return ItemViewModel.pUIh; } }

        private static ProgressUIPath pUIp = new ProgressUIPath();
        public static ProgressUIPath PUIP { get { return ItemViewModel.pUIp; } }

        private static ProgressUIButtonText buttonText = new ProgressUIButtonText();
        public static ProgressUIButtonText ButtonText { get { return ItemViewModel.buttonText; } }

        private static CollisionBoxHeader collisionBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader CollisionBoxHeader { get { return collisionBoxHeader; } }

        private static CollisionBoxSubHeader collisionBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader CollisionBoxSubHeader { get { return collisionBoxSubHeader; } }

        private static CollisionUIVisibility collisionUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility CollisionUIVisibility { get { return collisionUIVisibility; } }

        private static CollisionBoxHeader conflictBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader ConflictBoxHeader { get { return conflictBoxHeader; } }

        private static CollisionBoxSubHeader conflictBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader ConflictBoxSubHeader { get { return conflictBoxSubHeader; } }

        private static CollisionUIVisibility conflictUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility ConflictUIVisibility { get { return conflictUIVisibility; } }

        private static EmptyFolderTextState textState = new EmptyFolderTextState();
        public static EmptyFolderTextState TextState { get { return textState; } }

        public static int NumOfItems;
        public static int NumItemsRead;
        public static int NumOfFiles;
        public static int NumOfFolders;
        public static CancellationToken token;
        public static CancellationTokenSource tokenSource;

        public ItemViewModel(string ViewPath, Page p)
        {
            
            pageName = p.Name;
            // Personalize retrieved items for view they are displayed in
            if(p.Name == "GenericItemView" || p.Name == "ClassicModePage")
            {
                isPhotoAlbumMode = false;
            }
            else if (p.Name == "PhotoAlbumViewer")
            {
                isPhotoAlbumMode = true;
            }
            
            if(pageName != "ClassicModePage")
            {
                GenericFileBrowser.P.path = ViewPath;
                FilesAndFolders.Clear();
            }

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            MemoryFriendlyGetItemsAsync(ViewPath, token);

            if (pageName != "ClassicModePage")
            {
                History.AddToHistory(ViewPath);

                if (History.HistoryList.Count == 1)
                {
                    BS.isEnabled = false;
                    //Debug.WriteLine("Disabled Property");


                }
                else if (History.HistoryList.Count > 1)
                {
                    BS.isEnabled = true;
                    //Debug.WriteLine("Enabled Property");
                }
            }
            

        }

        private async void DisplayConsentDialog()
        {
            MessageDialog message = new MessageDialog("This app is not able to access your files. You need to allow it to by granting permission in Settings.");
            message.Title = "Permission Denied";
            message.Commands.Add(new UICommand("Allow...", new UICommandInvokedHandler(Interaction.GrantAccessPermissionHandler)));
            await message.ShowAsync();
        }
        string sort = "By_Name";
        SortEntry entry;
        public async void MemoryFriendlyGetItemsAsync(string path, CancellationToken token)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PUIP.Path = path;
            try
            {

                PVIS.isVisible = Visibility.Visible;
                TextState.isVisible = Visibility.Collapsed;
                folder = await StorageFolder.GetFolderFromPathAsync(path);
                QueryOptions options = new QueryOptions()
                {
                    FolderDepth = FolderDepth.Shallow,
                    IndexerOption = IndexerOption.UseIndexerWhenAvailable
                };

                if (sort == "By_Name")
                {
                    entry = new SortEntry()
                    {
                        AscendingOrder = true,
                        PropertyName = "System.FileName"
                    };
                }
                options.SortOrder.Add(entry);

                uint index = 0;
                const uint step = 250;
                if (!folder.AreQueryOptionsSupported(options))
                {
                    options.SortOrder.Clear();
                }

                StorageFolderQueryResult folderQueryResult = folder.CreateFolderQueryWithOptions(options);
                IReadOnlyList<StorageFolder> folders = await folderQueryResult.GetFoldersAsync(index, step);
                int foldersCountSnapshot = folders.Count;
                while (folders.Count != 0)
                {
                    foreach(StorageFolder folder in folders)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        gotFolName = folder.Name.ToString();
                        gotFolDate = folder.DateCreated.ToString();
                        gotFolPath = folder.Path.ToString();
                        gotFolType = "Folder";
                        gotFolImg = Visibility.Visible;
                        gotFileImgVis = Visibility.Collapsed;


                        if (pageName == "ClassicModePage")
                        {
                            ClassicFolderList.Add(new Classic_ListedFolderItem() { FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem() { ItemIndex = FilesAndFolders.Count, FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                    }
                    index += step;
                    folders = await folderQueryResult.GetFoldersAsync(index, step);
                }

                index = 0;
                StorageFileQueryResult fileQueryResult = folder.CreateFileQueryWithOptions(options);
                IReadOnlyList<StorageFile> files = await fileQueryResult.GetFilesAsync(index, step);
                int filesCountSnapshot = files.Count;
                while (files.Count != 0)
                {
                    foreach (StorageFile file in files)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        gotName = file.Name.ToString();
                        gotDate = file.DateCreated.ToString(); // In the future, parse date to human readable format
                        if (file.FileType.ToString() == ".exe")
                        {
                            gotType = "Executable";
                        }
                        else
                        {
                            gotType = file.DisplayType;
                        }
                        gotPath = file.Path.ToString();
                        gotFolImg = Visibility.Collapsed;
                        if (isPhotoAlbumMode == false)
                        {
                            const uint requestedSize = 20;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                            gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }
                        else
                        {
                            const uint requestedSize = 275;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.PicturesView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.ResizeThumbnail;
                            gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }

                        BitmapImage icon = new BitmapImage();
                        if (gotFileImg != null)
                        {
                            icon.SetSource(gotFileImg.CloneStream());
                        }
                        gotFileImgVis = Visibility.Visible;

                        if (pageName == "ClassicModePage")
                        {
                            ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                    }
                    index += step;
                    files = await fileQueryResult.GetFilesAsync(index, step);
                }
                if(foldersCountSnapshot + filesCountSnapshot == 0)
                {
                    TextState.isVisible = Visibility.Visible;
                }
                if (pageName != "ClassicModePage")
                {
                    PVIS.isVisible = Visibility.Collapsed;
                }
                PVIS.isVisible = Visibility.Collapsed;
            }        
            catch (UnauthorizedAccessException)
            {
                DisplayConsentDialog();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Drive Unplugged");
                await driveGone.ShowAsync();
                rootFrame.Navigate(typeof(MainPage), new SuppressNavigationTransitionInfo());
            }
            stopwatch.Stop();
            Debug.WriteLine("Loading of: " + path + " completed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
        }
        
        

        public static ProgressPercentage progressPER = new ProgressPercentage();

        public static ProgressPercentage PROGRESSPER
        {
            get
            {
                return progressPER;
            }
            set
            {

            }
        }

        public static int UpdateProgUI(int level)
        {
            PROGRESSPER.prog = level;
            return (int)level;
        }

        public static async void DisplayCollisionUIWithArgs(string header, string subHeader)
        {
            CollisionBoxHeader.Header = header;
            CollisionBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.collisionBox.ShowAsync();
        }

        public static async void DisplayReviewUIWithArgs(string header, string subHeader)
        {
            ConflictBoxHeader.Header = header;
            ConflictBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.reviewBox.ShowAsync();
        }

        public static async void FillTreeNode(object item, Microsoft.UI.Xaml.Controls.TreeView EntireControl)
        {
            var PathToFillFrom = (item as Classic_ListedFolderItem).FilePath;
            StorageFolder FolderFromPath = await StorageFolder.GetFolderFromPathAsync(PathToFillFrom);
            IReadOnlyList<StorageFolder> SubFolderList = await FolderFromPath.GetFoldersAsync();
            foreach(StorageFolder fol in SubFolderList)
            {
                var name = fol.Name;
                var date = fol.DateCreated.LocalDateTime.ToString();
                var ext = fol.DisplayType;
                var path = fol.Path;
                (item as Classic_ListedFolderItem).Children.Add(new Classic_ListedFolderItem() { FileName = name, FilePath = path, FileDate = date, FileExtension = ext});

            }
        }
    }
}
