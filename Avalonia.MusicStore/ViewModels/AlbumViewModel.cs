using Avalonia.Media.Imaging;
using Avalonia.MusicStore.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.MusicStore.ViewModels
{
    public class AlbumViewModel : ViewModelBase
    {
        private Bitmap? cover;

        public AlbumViewModel(Album album)
        {
            Album = album;
        }

        private Album Album { get; }

        public string Artist => Album.Artist;

        public Bitmap? Cover
        {
            get => cover;
            private set => this.RaiseAndSetIfChanged(ref cover, value);
        }

        public string Title => Album.Title;

        public async Task LoadCover()
        {
            await using (var imageStream = await Album.LoadCoverBitmapAsync())
            {
                Cover = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
            }
        }

        public async Task SaveToDiskAsync()
        {
            await Album.SaveAsync();

            if (Cover != null)
            {
                var bitmap = Cover;

                await Task.Run(() =>
                {
                    using (var fs = Album.SaveCoverBitmapStream())
                    {
                        bitmap.Save(fs);
                    }
                });
            }
        }

    }
}
