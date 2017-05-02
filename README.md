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

Шаг 2. Исходящие звонки кликом по номеру
----------------------------------------

Для начала в классе ProstieZvonki определим внутренний номер телефона сотрудника, от имени которого будет совершен звонок:

ProstieZvonki.cs

```cs
public class ProstieZvonki
{
	// внутренний номер телефона сотрудника, 
	// для которого будем обрабатывать события
	private const string UserNumber = "101";
	
	...
}
```

A также сам метод совершения исходящего вызова:

ProstieZvonki.cs

```cs
public class ProstieZvonki
{
	...

	public void Call(string phone)
	{
		var result = control.Call(
			UserNumber,      // внутренний номер сотрудника
			phone            // номер телефона, на который нужно позвонить
			);

		if (result != 0)
		{
			throw new ProstieZvonkiException(string.Format("Call returned bad result: {0}", result));
		}
	}
}
```

Затем определим в файле ViewModels.cs класс ProstieZvonkiCallCommand и добавим экземпляр этого класса в ProstieZvonkiState, чтобы иметь возможность совершать звонок через графический интерфейс:

ViewModels.cs

```cs
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
	public ProstieZvonkiCallCommand CallCommand { get; }
	
	...
}
```

Cделаем номера телефонов клиентов ссылками. Для этого заменим код, отвечающий за заполнение таблицы с контактами:

```xml
<TextBlock Grid.Row="0" Grid.Column="1" Padding="10" Width="Auto" FontSize="14" Text="{Binding Phone}"/>
```

на 

```xml
<Button Grid.Row="0" Grid.Column="1" Margin="5" Width="Auto" FontSize="14" Content="{Binding Phone}" Command="{Binding     RelativeSource={RelativeSource AncestorType={x:Type ItemsControl}}, Path=DataContext.State.CallCommand}" CommandParameter="{Binding Phone}">
<Button.Style>
	<Style TargetType="Button">
		<Setter Property="VerticalAlignment" Value="Center"/>
		<Setter Property="HorizontalAlignment" Value="Center"/>
		<Setter Property="Cursor" Value="Hand"/>
		<Setter Property="Foreground" Value="#FF1D60BF"/>
		<Setter Property="Background" Value="Transparent"/>
		<Setter Property="Template">
			<Setter.Value>
				<ControlTemplate TargetType="Button">
					<TextBlock Text="{TemplateBinding Content}" Background="{TemplateBinding Background}"/>
					<ControlTemplate.Triggers>
						<Trigger Property="IsPressed" Value="True">
							<Setter Property="Foreground" Value="#FFCB1C1C"/>
						</Trigger>
					</ControlTemplate.Triggers>
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>
</Button.Style>
</Button>
```

![Делаем телефоны ссылками](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/phone-links.png)

Кликнув на номер клиента, посмотрим на вывод тестового сервера:

```
Call event from CRM: src = 101, dst = +7 (343) 0112233
```

Как мы видим, сервер получил запрос на создание исходящего звонка с номера 101 на номер +7 (343) 0112233.

Шаг 3. Всплывающая карточка входящего звонка
--------------------------------------------

Для начала научимся обрабатывать события о входящих звонках от сервера "Простых Звонков". Для этого в классе ProstieZvonki подпишемся на события для нашего внутреннего номера и добавим обработчик события OnTransferredCall:

ProstieZvonki.cs

```cs
public class ProstieZvonki
{
	// будем оповещать "внешний" код о наступивших событиях 
	public delegate void TransferredCallEventHandler(string src, string dst);
	public event TransferredCallEventHandler TransferredCallEvent;

    ...
	
	private ProstieZvonki()
	{
	    ...
		
		// подписываемся на события для нашего внутреннего номера
		control.phoneNumber = UserNumber;

		// обрабатываем нужные события
		control.OnTransferredCall += OnTransferredCall;
	}
	
	private void OnTransferredCall(string callID, string src, string dst, string line)
    {
        TransferredCallEvent(src, dst);
    }
	
    ...
}
```

В класс ProstieZvonkiState добавим обработчик события TransferredCallEvent класса ProstieZvonki. В этом обработчике воспользуемся стандартным диалоговым окном для вывода информации о входящем звонке:

```cs
public class ProstieZvonkiState : INotifyPropertyChanged
{
	private ContactsStorage contactsStorage;

    ...

	public ProstieZvonkiState(ContactsStorage contacts)
	{
		contactsStorage = contacts;

	    ...
		
		ProstieZvonki.Instance.TransferredCallEvent += OnTransferredCall;
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
	
    ...
}
```

> Как видите, мы воспользовались вспомогательной функцией для очистки номера телефона от посторонних символов и кода страны. Таким образом, поиск по номерам `+7 (343) 0112233` и `83430112233` будет выдавать одинаковый результат, что нам и нужно.

Чтобы проверить работу всплывающей "карточки", создадим входящий звонок с номера 73430112233 на номер 101 с помощью диагностической утилиты Diagnostic.exe:

```
[events off]> Generate transfer 73430112233 101
```

Приложение должно незамедлительно отобразить модальное диалоговое окно:

![Карточка входящего звонка](https://github.com/vedisoft/activex-integration-tutorial/raw/master/img/incoming-popup.png)
