using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
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

    public class CallHistoryInfo
    {
        public string Direction { get; }
        public string Phone { get; }
        public string Name { get; }
        public string StartTime { get; }
        public string Duration { get; }

        public CallHistoryInfo(bool isIncoming, string phone, string name, int startTime, int duration)
        {
            Direction = isIncoming ? "Входящий" : "Исходящий";
            Phone = phone;
            Name = name;

            var datetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            datetime = datetime.AddSeconds(startTime).ToLocalTime();
            StartTime = datetime.ToString(CultureInfo.InvariantCulture);

            var time = TimeSpan.FromSeconds(duration);
            Duration = time.ToString();
        }
    }

    public class CallHistoryStorage
    {
        public ObservableCollection<CallHistoryInfo> Items { get; }

        public CallHistoryStorage()
        {
            Items = new ObservableCollection<CallHistoryInfo>();
        }

        public void AddRecord(CallHistoryInfo info)
        {
            // будем хранить последние три звонка
            if (Items.Count > 2)
            {
                Items.RemoveAt(0);
            }

            Items.Add(info);
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
        private ContactsStorage contactsStorage;
        private CallHistoryStorage callHistoryStorage;

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

        public ProstieZvonkiState(ContactsStorage contacts, CallHistoryStorage history)
        {
            contactsStorage = contacts;
            callHistoryStorage = history;

            ConnectCommand = new ProstieZvonkiConnectCommand();
            CallCommand = new ProstieZvonkiCallCommand();

            ProstieZvonki.Instance.ConnectionStateChangedEvent += OnConnStatusChange;
            ProstieZvonki.Instance.TransferredCallEvent += OnTransferredCall;
            ProstieZvonki.Instance.CompletedCallEvent += OnCompletedCall;
            ProstieZvonki.Instance.TransferRequestEvent += OnTransferRequest;
        }

        private void OnConnStatusChange(bool isConnected)
        {
            OnPropertyChanged("IsConnected");
        }

        private void OnTransferredCall(string src, string dst)
        {
            var button = MessageBoxButton.OK;
            var icon = MessageBoxImage.Information;
            var name = FindContactName(src);
            var caption = "TinyCRM";
            var text = string.Format("Звонок{0}", name != string.Empty ? string.Format(": {0}", name) : 
                string.Format(" c неизвестного номера {0}", src));

            MessageBox.Show(Application.Current.MainWindow, text, caption, button, icon);
        }

        private void OnCompletedCall(bool isIncoming, string src, string dst, int start, int duration)
        {
            var phone = isIncoming ? src : dst;
            var name = FindContactName(phone);

            callHistoryStorage.AddRecord(new CallHistoryInfo(isIncoming, phone, name, start, duration));
        }

        private void OnTransferRequest(string callID, string from)
        {
            var name = FindContactName(from);
            if (name == string.Empty)
            {
                return;
            }

            ProstieZvonki.Instance.Transfer(callID);
        }

        private string FindContactName(string phone)
        {
            var name = string.Empty;
            var refined = RefinedPhone(phone);

            foreach (var contact in contactsStorage.Items)
            {
                if (RefinedPhone(contact.Phone) == refined)
                {
                    name = contact.Name;
                    break;
                }
            }

            return name;
        }

        private string RefinedPhone(string phone)
        {
            // приводим телефонные номера к единой форме для поиска в базе контактов
            var result = Regex.Replace(phone, "[^0-9]", "");

            var phoneMaxLen = 10;
            return result.Substring(result.Length > phoneMaxLen ? result.Length - phoneMaxLen : 0);
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
        public CallHistoryStorage CallHistory { get; }
        public ProstieZvonkiState State { get; }

        public MainViewModel()
        {
            Contacts = new ContactsStorage();
            CallHistory = new CallHistoryStorage();
            State = new ProstieZvonkiState(Contacts, CallHistory);
        }
    }
}
