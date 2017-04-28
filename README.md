Пример интеграции ActiveX-компонента "Простых Звонков" с Windows-приложением
============================================================================

Простые Звонки - сервис для интеграции клиентских приложений (Excel, 1C и ERP-cистем) с офисными и облачными АТС. Клиентское приложение может общаться с сервером "Простых Звонков" через единый API, независимо от типа используемой АТС. 

В данном примере мы рассмотрим процесс подключения к серверу "Простых Звонков" Windows-приложения, написанного на С#(можно использовать любой язык программирования, поддерживающий ActiveX-компоненты). Мы начнём с приложения, выводящего на экран список клиентов из базы данных, и добавим в него следующие функции:

- отображение всплывающей карточки при входящем звонке
- звонок клиенту по клику на телефонный номер
- история входящих и исходящих звонков
- умная переадресация на менеджера клиента

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
