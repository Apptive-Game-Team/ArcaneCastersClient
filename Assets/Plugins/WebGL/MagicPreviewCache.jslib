mergeInto(LibraryManager.library, {
  SyncMagicPreviewCache: function () {
    FS.syncfs(false, function (error) {
      if (error) console.warn('Magic preview file cache could not be persisted.');
    });
  }
});
