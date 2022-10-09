using ReactiveUI;
using System;
using System.Reactive.Linq;
using Todo.Models;
using Todo.Services;

namespace Todo.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase content;

        public MainWindowViewModel(Database db)
        {
            List = new TodoListViewModel(db.GetItems());
            Content = List;
        }

        public ViewModelBase Content
        { 
            get => content;
            private set => this.RaiseAndSetIfChanged(ref content, value);
        }

        public TodoListViewModel List { get; }

        public void AddItem()
        {
            var vm = new AddItemViewModel();

            Observable.Merge(
                vm.Ok,
                vm.Cancel.Select(_ => (TodoItem)null))
                .Take(1)
                .Subscribe(model =>
                {
                    if (model != null)
                    {
                        List.Items.Add(model);
                    }

                    Content = List;
                });

            Content = vm;
        }
    }
}
