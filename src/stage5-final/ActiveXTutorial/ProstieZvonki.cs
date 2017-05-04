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

        public delegate void TransferredCallEventHandler(string src, string dst);
        public event TransferredCallEventHandler TransferredCallEvent;

        public delegate void CompletedCallEventHandler(bool isIncoming, string src, string dst, int start, int duration);
        public event CompletedCallEventHandler CompletedCallEvent;

        public delegate void TransferRequestEventHandler(string callID, string from);
        public event TransferRequestEventHandler TransferRequestEvent;

        // внутренний номер телефона сотрудника, 
        // для которого будем обрабатывать события
        private const string UserNumber = "101";

        // ссылка на ActiveX-компонент, которая используется для работы с сервисом "Простых Звонков"
        private CTIControlX control;

        // сохраненное состояние соединения с сервером
        public bool IsConnected { get; private set; }

        // используем паттерн Singleton для доступа к функциональности, 
        // предоставляемой "Простыми Звонками", из "внешнего" кода
        private static ProstieZvonki instance;

        private ProstieZvonki()
        {
            control = new CTIControlX();
            // подписываемся на события для нашего внутреннего номера
            control.phoneNumber = UserNumber;

            IsConnected = false;

            // обрабатываем нужные события
            control.OnConnectionState += OnConnectionState;
            control.OnTransferredCall += OnTransferredCall;
            control.OnCompletedCall += OnCompletedCall;
            control.OnTransferRequest += OnTransferRequest;
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
                "guid",                     // Уникальный идентификатор пользователя CRM системы. 
                                            // Должен отличаться для разных пользователей CRM системы. 
                                            // Должен оставаться постоянным при каждом подключении одного и того же 
                                            // пользователя к АТС-коннектору "Простых звонков".
                "log.txt",                  // Путь к файлу лога. Данный путь должен быть доступен для записи, 
                                            // иначе функция вернет ошибку и соединение не произойдет
                0,                          // Специфичные флаги управления работой, всегда 0
                500                         // Интервал между попытками переподключения к серверу в миллисекундах
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

        public void Transfer(string callId)
        {
            var result = control.Transfer(
                callId,         // идентификационный номер звонка
                UserNumber      // внутренний номер сотрудника
                );

            if (result != 0)
            {
                throw new ProstieZvonkiException(string.Format("Transfer returned bad result: {0}", result));
            }
        }

        private void OnConnectionState(int state)
        {
            IsConnected = (state == 1);
            ConnectionStateChangedEvent(IsConnected);
        }

        private void OnTransferredCall(string callID, string src, string dst, string line)
        {
            TransferredCallEvent(src, dst);
        }

        private void OnCompletedCall(string callID, string src, string dst, int duration, string start, string end, int direction, string record, string line)
        {
            var isIncoming = direction == 0;
            var timestamp = Convert.ToInt32(start);

            CompletedCallEvent(isIncoming, src, dst, timestamp, duration);
        }

        private void OnTransferRequest(string callID, string from, string line)
        {
            TransferRequestEvent(callID, from);
        }
    }
}
