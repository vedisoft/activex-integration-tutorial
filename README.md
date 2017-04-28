Пример интеграции ActiveX-компонента "Простых Звонков" с Windows-приложением
============================================================================

Простые Звонки - сервис для интеграции клиентских приложений (Excel, 1C и ERP-cистем) с офисными и облачными АТС. Клиентское приложение может общаться с сервером "Простых Звонков" через единый API, независимо от типа используемой АТС. 

В данном примере мы рассмотрим процесс подключения к серверу "Простых Звонков" Windows-приложения, написанного на С#(можно использовать любой язык программирования, поддерживающий ActiveX-компоненты). Мы начнём с приложения, выводящего на экран список клиентов из базы данных, и добавим в него следующие функции:

- отображение всплывающей карточки при входящем звонке
- звонок клиенту по клику на телефонный номер
- история входящих и исходящих звонков
- умная переадресация на менеджера клиента

В качестве среды разработки используется Visual Studio 2015

Шаг 0. Исходное приложение
--------------------------

Наше исходное приложение умеет показывать список клиентов. В качестве базы данных используется коллекция объектов класса Сontact, определяемая в файле ViewModels.cs. Объекты класса Сontact отображаются в виде таблицы.

ViewModels.cs:

```cs
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
```

MainWindow.xaml:

```xml
<ItemsControl Margin="0,10,0,0" DataContext="{StaticResource MainViewModel}" ItemsSource="{Binding Contacts.Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <Border Grid.Row="0" Grid.Column="0" Margin="0,-1,0,0" BorderBrush="Silver" BorderThickness="1">
                    <TextBlock Padding="10" HorizontalAlignment="Left" FontSize="14" Text="{Binding Name}"/>
                </Border>
                <Border Grid.Row="0" Grid.Column="1" Margin="-1,-1,0,0"  BorderBrush="Silver" BorderThickness="1">
                    <TextBlock Grid.Row="0" Grid.Column="1" Padding="10" Width="Auto" FontSize="14" Text="{Binding Phone}"/>
                </Border>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

![Исходное приложение](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/tinycrm-origin.png)

Шаг 1. Настройка подключения к серверу
--------------------------------------

Для начала необходимо скачать ActiveX-компонент по ссылке [отсюда](http://prostiezvonki.ru/installs/ProstieZvonki_ActiveX_2.0.exe)

После установки потребуется подключить ActiveX-компонент к проекту:

![Подключаем ActiveX_1](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/references_1.png)
![Подключаем ActiveX_2](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/references_2.png)
![Подключаем ActiveX_3](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/references_3.png)
![Подключаем ActiveX_4](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/references_4.png)
![Подключаем ActiveX_5](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/references_5.png)

Теперь нужно скачать [тестовый сервер и диагностическую утилиту](https://github.com/vedisoft/pz-developer-tools).

Запустим тестовый сервер:

    > TestServer.exe

и подключимся к нему диагностической утилитой:

    > Diagnostic.exe

    [events off]> Connect ws://localhost:10150 asd
    * Далее приложение запросит ввести пароль, просто нажмите Enter
    Успешно начато установление соединения с АТС

Тестовое окружение настроено. Следующим шагом станет добавление класса ProstieZvonki для взаимодействия с ActiveX-компонентом.

ProstieZvonki.cs:

```cs
using System;
using CTIControlLib;

namespace ActiveXTutorial
{
    public class ProstieZvonkiException : Exception
    {
        public ProstieZvonkiException(string message) 
            : base(message)
        {

        }
    }

    public class ProstieZvonki
    {
        // будем оповещать "внешний" код о наступивших событиях 
        public delegate void ConnectionStateChangedEventHandler(bool isConnected);
        public event ConnectionStateChangedEventHandler ConnectionStateChangedEvent;

        // объект используется для вызова методов "Простых Звонков"
        private CTIControlX control;

        // сохранненое состояние соединения с сервером
        public bool IsConnected { get; private set; }

        // используем паттерн Singleton для доступа к функциональности, 
        // предоставляемой "Простыми Звонками", из "внешнего" кода
        private static ProstieZvonki instance;

        private ProstieZvonki()
        {
            control = new CTIControlX();

            IsConnected = false;

            // подписываемся на нужные события
            control.OnConnectionState += OnConnectionState;
        }

        public static ProstieZvonki Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProstieZvonki();
                }

                return instance;
            }
        }

        public void Connect()
        {
            var result = control.Connect(
                "ws://127.0.0.1:10150",     // Адрес АТС коннектора
                "password",                 // Пароль для доступа к АТС коннектору
                "activex",                  // Тип клиента
                "guid",                     // Уникальный случайно сгенерированный GUID модуля CRM
                "log.txt",                  // Путь к файлу лога. Данный путь должен быть доступен для записи, 
                                            // иначе функция вернет ошибку и соединение не произойдет
                0,                          // Специфичные флаги управления работой, всегда 0
                100                         // Интервал между попытками переподключения к серверу в миллисекундах
                );

            if (result != 0)
            {
                throw new ProstieZvonkiException(string.Format("Connect returned bad result: {0}", result));
            }
        }

        public void Disconnect()
        {
            var result = control.Disconnect();
            if (result != 0)
            {
                throw new ProstieZvonkiException(string.Format("Disconnect returned bad result: {0}", result));
            }
        }

        private void OnConnectionState(int state)
        {
            IsConnected = (state == 1);
            ConnectionStateChangedEvent(IsConnected);
        }
    }
}
```

Добавим модель представления ProstieZvonkiState, посредством которой представление будет взаимодействовать с ActiveX-компонентом:

ViewModels.cs:

```cs
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

    public ProstieZvonkiState()
    {
        ConnectCommand = new ProstieZvonkiConnectCommand();

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
```

Добавим в наше приложение кнопку для соединения с сервером "Простых Звонков" и индикатор состояния:

MainWindow.xaml:

```xml
<Grid Margin="0,20,0,0" DataContext="{StaticResource MainViewModel}">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>

    <Border Grid.Row="0" Grid.Column="0" CornerRadius="10" Margin="0,0,0,10">
        <Border.Resources>
            Style TargetType="TextBlock">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding State.IsConnected}" Value="True">
                        <Setter Property="Text" Value="Соединение установлено"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding State.IsConnected}" Value="False">
                        <Setter Property="Text" Value="Нет соединения"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
            <Style TargetType="Border">
                <Style.Triggers>
                    <DataTrigger Binding="{Binding State.IsConnected}" Value="True">
                        <Setter Property="Background" Value="#FF419541"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding State.IsConnected}" Value="False">
                        <Setter Property="Background" Value="#FFB2B2B2"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Resources>
        <TextBlock Padding="8,2,8,2" HorizontalAlignment="Left" Foreground="White" FontSize="12"/>
    </Border>
    <Button Grid.Row="0" Grid.Column="2" Width="90" Command="{Binding State.ConnectCommand}" CommandParameter="{Binding State.IsConnected}">
        <Button.Style>
            <Style TargetType="{x:Type Button}">
                <Style.Triggers>
                    DataTrigger Binding="{Binding State.IsConnected}" Value="True">
                        <Setter Property="Content" Value="Разъединить"/>
                    </DataTrigger>
                    <DataTrigger Binding="{Binding State.IsConnected}" Value="False">
                        <Setter Property="Content" Value="Соединить"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
    </Button>
</Grid>
```

Теперь наше приложение выглядит так:

![Индикатор состояния соединения](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/connection-indicator.png)

Попробуем подключиться к серверу:

![Соединение установлено](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/connection-established.png)
