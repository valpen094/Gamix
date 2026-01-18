using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Gamix.UI.ViewModels
{
    /// <summary>
    /// テーマ選択ダイアログ用の ViewModel。
    /// ページネーション機能を備えたテーマ一覧表示と選択を提供します。
    /// </summary>
    public partial class ThemeSelectionViewModel : ObservableObject
    {
        #region Constants

        private const int ItemsPerPage = 2;
        private const string ImagePathFormat = "/Gamix;component/Resources/Images/{0}_theme_preview.png";
        private const string PackUriFormat = "pack://application:,,,/Gamix;component/Resources/Images/{0}_theme_preview.png";

        #endregion

        #region Properties

        /// <summary>
        /// 全てのテーマオプション。
        /// </summary>
        public ObservableCollection<ThemeOption> ThemeOptions { get; } = new();

        /// <summary>
        /// 現在のページに表示するテーマオプション。
        /// </summary>
        public ObservableCollection<ThemeOption> PagedThemeOptions { get; } = new();

        /// <summary>
        /// ページインジケーター用のドットコレクション。
        /// </summary>
        public ObservableCollection<PageDot> PageDots { get; } = new();

        /// <summary>
        /// 現在のページ番号 (1-indexed)。
        /// </summary>
        [ObservableProperty]
        private int _currentPage = 1;

        /// <summary>
        /// 総ページ数。
        /// </summary>
        [ObservableProperty]
        private int _totalPages = 1;

        #endregion

        #region Fields

        private readonly Action<string>? _onApplyTheme;

        #endregion

        #region Constructor

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="availableThemes">利用可能なテーマ名のリスト。</param>
        /// <param name="currentTheme">現在選択中のテーマ名。</param>
        /// <param name="onApplyTheme">テーマ適用時のコールバック。</param>
        public ThemeSelectionViewModel(
            IEnumerable<string> availableThemes, 
            string currentTheme, 
            Action<string>? onApplyTheme = null)
        {
            _onApplyTheme = onApplyTheme;
            InitializeThemeOptions(availableThemes, currentTheme);
            RefreshPagination();
        }

        #endregion

        #region Initialization

        private void InitializeThemeOptions(IEnumerable<string> availableThemes, string currentTheme)
        {
            foreach (var theme in availableThemes)
            {
                ThemeOptions.Add(new ThemeOption
                {
                    Name = theme,
                    ImagePath = GetThemeImagePath(theme),
                    IsSelected = theme == currentTheme
                });
            }
        }

        #endregion

        #region Pagination

        private void RefreshPagination()
        {
            CalculateTotalPages();
            ClampCurrentPage();
            UpdatePagedItems();
            UpdatePageDots();
            NotifyNavigationCommands();
        }

        private void CalculateTotalPages()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)ThemeOptions.Count / ItemsPerPage));
        }

        private void ClampCurrentPage()
        {
            CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);
        }

        private void UpdatePagedItems()
        {
            PagedThemeOptions.Clear();
            var items = ThemeOptions
                .Skip((CurrentPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage);

            foreach (var item in items)
            {
                PagedThemeOptions.Add(item);
            }
        }

        private void UpdatePageDots()
        {
            PageDots.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageDots.Add(new PageDot(i, isActive: i == CurrentPage));
            }
        }

        private void NotifyNavigationCommands()
        {
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #region Commands

        /// <summary>
        /// 次のページに移動します。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private void NextPage()
        {
            CurrentPage++;
            RefreshPagination();
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;

        /// <summary>
        /// 前のページに移動します。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private void PreviousPage()
        {
            CurrentPage--;
            RefreshPagination();
        }

        private bool CanGoToPreviousPage() => CurrentPage > 1;

        /// <summary>
        /// テーマを選択します。
        /// </summary>
        [RelayCommand]
        private void SelectTheme(ThemeOption? option)
        {
            if (option == null) return;

            foreach (var item in ThemeOptions)
            {
                item.IsSelected = item == option;
            }

            // 選択と同時にテーマを適用
            _onApplyTheme?.Invoke(option.Name);
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// テーマ画像をあらかじめメモリに読み込み、デコードを完了させます。
        /// </summary>
        public static async Task PreloadImagesAsync(IEnumerable<string> availableThemes)
        {
            await Task.Run(() =>
            {
                foreach (var theme in availableThemes)
                {
                    TryPreloadImage(theme);
                }
            });
        }

        private static void TryPreloadImage(string themeName)
        {
            try
            {
                var uri = new Uri(GetThemePackUri(themeName), UriKind.Absolute);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                });
            }
            catch
            {
                // 読み込み失敗は無視
            }
        }

        private static string GetThemeImagePath(string themeName)
            => string.Format(ImagePathFormat, themeName.ToLowerInvariant());

        private static string GetThemePackUri(string themeName)
            => string.Format(PackUriFormat, themeName.ToLowerInvariant());

        #endregion
    }

    /// <summary>
    /// テーマ選択オプションを表すモデル。
    /// </summary>
    public partial class ThemeOption : ObservableObject
    {
        /// <summary>
        /// テーマ名。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// プレビュー画像のパス。
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// 選択状態。
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>
    /// ページインジケーター用のドットを表すモデル。
    /// </summary>
    public partial class PageDot : ObservableObject
    {
        /// <summary>
        /// ページ番号。
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// アクティブ状態（現在のページかどうか）。
        /// </summary>
        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public PageDot(int pageNumber, bool isActive = false)
        {
            PageNumber = pageNumber;
            IsActive = isActive;
        }
    }
}
