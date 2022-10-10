using System;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using Avalonia.MusicStore.Models;
using System.Linq;
using System.Reactive.Concurrency;

namespace Avalonia.MusicStore.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool collectionEmpty;

        public MainWindowViewModel()
        {
            ShowDialog = new Interaction<MusicStoreViewModel, AlbumViewModel?>();

            BuyMusicCommand = ReactiveCommand.Create(async () =>
            {
                var store = new MusicStoreViewModel();

                var result = await ShowDialog.Handle(store);

                if (result != null)
                {
                    Albums.Add(result);

                    await result.SaveToDiskAsync();
                }
            });

            this.WhenAnyValue(x => x.Albums.Count)
                .Subscribe(x => CollectionEmpty = x == 0);

            RxApp.MainThreadScheduler.Schedule(LoadAlbums);
        }

        public ObservableCollection<AlbumViewModel> Albums { get; } = new();

        public ICommand BuyMusicCommand { get; }

        public bool CollectionEmpty
        {
            get => collectionEmpty;
            set => this.RaiseAndSetIfChanged(ref collectionEmpty, value);
        }

        public Interaction<MusicStoreViewModel, AlbumViewModel?> ShowDialog { get; }

        private async void LoadAlbums()
        {
            var albums = (await Album.LoadCachedAsync()).Select(x => new AlbumViewModel(x));

            foreach (var album in albums)
            {
                Albums.Add(album);
            }

            foreach (var album in Albums.ToList())
            {
                await album.LoadCover();
            }
        }
    }
}
