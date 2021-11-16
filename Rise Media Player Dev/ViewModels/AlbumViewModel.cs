﻿using Rise.App.Common;
using Rise.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rise.App.ViewModels
{
    public class AlbumViewModel : ViewModel<Album>
    {
        // private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Initializes a new instance of the AlbumViewModel class that wraps an Album object.
        /// </summary>
        public AlbumViewModel(Album model = null)
        {
            Model = model ?? new Album();
            IsNew = true;
            OnPropertyChanged(nameof(ArtistViewModel.AlbumCount));
        }

        /// <summary>
        /// Gets or sets the album title.
        /// </summary>
        public string Title
        {
            get
            {
                if (Model.Title == "UnknownAlbumResource")
                {
                    return ResourceLoaders.MediaDataLoader.GetString("UnknownAlbumResource");
                }

                return Model.Title;
            }
            set
            {
                if (value != Model.Title)
                {
                    Model.Title = value;
                    IsModified = true;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        public string Artist
        {
            get
            {
                if (Model.Artist == "UnknownArtistResource")
                {
                    return ResourceLoaders.MediaDataLoader.GetString("UnknownArtistResource");
                }

                return Model.Artist;
            }
            set
            {
                if (value != Model.Artist)
                {
                    Model.Artist = value;
                    IsModified = true;
                    OnPropertyChanged(nameof(Artist));
                }
            }
        }

        /// <summary>
        /// Gets or sets the album genre.
        /// </summary>
        public string Genres
        {
            get
            {
                if (Model.Genres == "UnknownGenreResource")
                {
                    return ResourceLoaders.MediaDataLoader.GetString("UnknownGenreResource");
                }

                return Model.Genres;
            }
            set
            {
                if (value != Model.Genres)
                {
                    Model.Genres = value;
                    IsModified = true;
                    OnPropertyChanged(nameof(Genres));
                }
            }
        }

        /// <summary>
        /// Gets or sets the album song count.
        /// </summary>
        public int TrackCount =>
            App.MViewModel.Songs.Count(s => s.Album == Model.Title);

        /// <summary>
        /// Gets or sets the album thumbnail.
        /// </summary>
        public string Thumbnail
        {
            get => Model.Thumbnail;
            set
            {
                if (value != Model.Thumbnail)
                {
                    Model.Thumbnail = value;
                    IsModified = true;
                    OnPropertyChanged(nameof(Thumbnail));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the underlying model has been modified. 
        /// </summary>
        /// <remarks>
        /// Used to reduce load and only upsert the models that have changed.
        /// </remarks>
        public bool IsModified { get; set; }
        
        private bool _isLoading;
        /// <summary>
        /// Gets or sets a value that indicates whether to show a progress bar. 
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        private bool _isNew;
        /// <summary>
        /// Gets or sets a value that indicates whether this is a new item.
        /// </summary>
        public bool IsNew
        {
            get => _isNew;
            set => Set(ref _isNew, value);
        }

        private bool _isInEdit = false;

        /// <summary>
        /// Gets or sets a value that indicates whether the album data is being edited.
        /// </summary>
        public bool IsInEdit
        {
            get => _isInEdit;
            set => Set(ref _isInEdit, value);
        }

        /// <summary>
        /// Saves album data that has been edited.
        /// </summary>
        public async Task SaveAsync()
        {
            IsInEdit = false;
            IsModified = false;

            if (IsNew)
            {
                IsNew = false;
                App.MViewModel.Albums.Add(this);
            }

            await App.Repository.Albums.QueueUpsertAsync(Model);
        }

        /// <summary>
        /// Checks whether or not the album is available. If it's not,
        /// delete it.
        /// </summary>
        public async Task CheckAvailabilityAsync()
        {
            if (TrackCount == 0)
            {
                await DeleteAsync();
                return;
            }
        }

        /// <summary>
        /// Delete album from repository and MViewModel.
        /// </summary>
        public async Task DeleteAsync()
        {
            IsModified = true;

            App.MViewModel.Albums.Remove(this);
            await App.Repository.Albums.QueueDeletionAsync(Model);

            ArtistViewModel artist = App.MViewModel.Artists.
                FirstOrDefault(a => a.Model.Name == Model.Artist);

            if (artist != null)
            {
                await artist.CheckAvailabilityAsync();
            }
        }

        /// <summary>
        /// Raised when the user cancels the changes they've made to the album data.
        /// </summary>
        public event EventHandler AddNewAlbumCanceled;

        /// <summary>
        /// Cancels any in progress edits.
        /// </summary>
        public async Task CancelEditsAsync()
        {
            if (IsNew)
            {
                AddNewAlbumCanceled?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                await RevertChangesAsync();
            }
        }

        /// <summary>
        /// Discards any edits that have been made, restoring the original values.
        /// </summary>
        public async Task RevertChangesAsync()
        {
            IsInEdit = false;
            if (IsModified)
            {
                await RefreshAlbumsAsync();
                IsModified = false;
            }
        }

        /// <summary>
        /// Reloads all of the album data.
        /// </summary>
        public async Task RefreshAlbumsAsync()
        {
            Model = await App.Repository.Albums.GetAsync(Model.Id);
        }
    }
}
