
using Avalonia.MusicStore.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace Avalonia.MusicStore.ViewModels
{
    public class MusicStoreViewModel : ViewModelBase
    {
        private bool isBusy;
        private string? searchText;
        private AlbumViewModel? selectedAlbum;
        private CancellationTokenSource? cancellationTokenSource;

        public MusicStoreViewModel()
        {
            BuyMusicCommand = ReactiveCommand.Create(() =>
            {
                return SelectedAlbum;
            });

            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(400))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(DoSearch!);
        }

        public ReactiveCommand<Unit, AlbumViewModel?> BuyMusicCommand { get; }

        public AlbumViewModel? SelectedAlbum
        {
            get => selectedAlbum;
            set => this.RaiseAndSetIfChanged(ref selectedAlbum, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public ObservableCollection<AlbumViewModel> SearchResults { get; } = new();

        public string? SearchText
        {
            get => searchText;
            set => this.RaiseAndSetIfChanged(ref searchText, value);
        }

        private async void DoSearch(string s)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            IsBusy = true;
            SearchResults.Clear();

            if (!string.IsNullOrWhiteSpace(s))
            {
                var albums = await Album.SearchAsync(s);

                foreach(var album in albums)
                {
                    var vm = new AlbumViewModel(album);
                    SearchResults.Add(vm);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    LoadCovers(cancellationToken);
                }
            }

            IsBusy = false;
        }

        private async void LoadCovers(CancellationToken cancellationToken)
        {
            foreach (var album in SearchResults.ToList())
            {
                await album.LoadCover();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
