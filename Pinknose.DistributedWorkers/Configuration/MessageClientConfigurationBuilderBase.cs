///////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2020 Cameron Mease
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////

namespace Pinknose.DistributedWorkers.Configuration
{
    public abstract class MessageClientConfigurationBuilderBase
    {
    }

    public abstract class MessageClientConfigurationBase<TConfigType> : MessageClientConfigurationBuilderBase where TConfigType : MessageClientConfigurationBuilderBase
    {
        #region Fields

        protected string _userName = "";
        protected string _password = "";

        protected string _rabbitMQServerName = "";

        protected bool _queuesAreDurable = true;
        protected bool _autoDeleteQueuesOnClose = false;

        #endregion Fields

        #region Methods

        public TConfigType Credentials(string userName, string password)
        {
            _userName = userName;
            _password = password;

            return (TConfigType)(object)this;
        }

        public TConfigType RabbitMQServer(string serverName)
        {
            _rabbitMQServerName = serverName;

            return (TConfigType)(object)this;
        }

        public TConfigType AutoDeleteQueuesOnClose(bool value)
        {
            _autoDeleteQueuesOnClose = value;
            return (TConfigType)(object)this;
        }

        public TConfigType QueuesAreDurable(bool value)
        {
            _queuesAreDurable = value;
            return (TConfigType)(object)this;
        }

        #endregion Methods
    }
}