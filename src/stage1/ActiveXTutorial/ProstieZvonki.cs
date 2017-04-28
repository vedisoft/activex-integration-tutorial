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
