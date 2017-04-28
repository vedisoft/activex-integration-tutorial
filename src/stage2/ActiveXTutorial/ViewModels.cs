using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ActiveXTutorial
{
    public class Contact
    {
        public string Name { get; }
        public string Phone { get; }

        public Contact(string name, string phone)
        {
            Name = name;
            Phone = phone;
        }
    }

    public class ContactsStorage
    {
        public Collection<Contact> Items { get; }

        public ContactsStorage()
        {
            Items = new Collection<Contact>();
            populateContacts();
        }

        private void populateContacts()
        {
            Items.Add(new Contact("Aркадий", "+7 (343) 0112233"));
            Items.Add(new Contact("Борис", "+7 (343) 0112244"));
            Items.Add(new Contact("Валентина", "+7 (343) 0112255"));
        }
    }

    public class ProstieZvonkiConnectCommand : ICommand
    {
        public ProstieZvonkiConnectCommand()
        {

        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            bool isConnected = (bool)parameter;
            if (!isConnected)
            {
                ProstieZvonki.Instance.Connect();
            }
            else
            {
                ProstieZvonki.Instance.Disconnect();
            }
        }
    }

    public class ProstieZvonkiCallCommand : ICommand
    {
        public ProstieZvonkiCallCommand()
        {

        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return ProstieZvonki.Instance.IsConnected;
        }

        public void Execute(object parameter)
        {
            var phone = (string)parameter;
            ProstieZvonki.Instance.Call(phone);
        }
    }

    public class ProstieZvonkiState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnected
        {
            get
            {
                return ProstieZvonki.Instance.IsConnected;
            }
        }

        public ProstieZvonkiConnectCommand ConnectCommand { get; }
        public ProstieZvonkiCallCommand CallCommand { get; }

        public ProstieZvonkiState()
        {
            ConnectCommand = new ProstieZvonkiConnectCommand();
            CallCommand = new ProstieZvonkiCallCommand();

            ProstieZvonki.Instance.ConnectionStateChangedEvent += OnConnStatusChange;
        }

        private void OnConnStatusChange(bool isConnected)
        {
            OnPropertyChanged("IsConnected");
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class MainViewModel
    {
        public ContactsStorage Contacts { get; }
        public ProstieZvonkiState State { get; }

        public MainViewModel()
        {
            Contacts = new ContactsStorage();
            State = new ProstieZvonkiState();
        }
    }
}
