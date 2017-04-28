using System.Collections.ObjectModel;

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

    public class MainViewModel
    {
        public ContactsStorage Contacts { get; }

        public MainViewModel()
        {
            Contacts = new ContactsStorage();
        }
    }
}
