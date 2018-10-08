﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Configuration-Environment containing sections of configuration that are shared among many <see cref="ConfigStructure" />
    /// </summary>
    public class ConfigEnvironment : DomainObject
    {
        private EnvironmentIdentifier _identifier;
        private bool _isDefault;

        /// <summary>
        ///     set the identifier of this <see cref="ConfigEnvironment"/> to the given value
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        public ConfigEnvironment IdentifiedBy(EnvironmentIdentifier identifier, bool isDefault = false)
        {
            _identifier = identifier;
            if (isDefault)
            {
                _isDefault = true;
                _identifier.Name = "Default";
            }

            return this;
        }

        /// <summary>
        ///     set the identifier of this <see cref="ConfigEnvironment"/> to
        ///     the correct value for a Default-Environment in the given category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public ConfigEnvironment DefaultIdentifiedBy(string category) => IdentifiedBy(new EnvironmentIdentifier(category, "Default"), true);

        /// <summary>
        ///     create Events that Create this DomainObject when saved
        /// </summary>
        /// <returns></returns>
        public ConfigEnvironment Create()
        {
            if (_isDefault)
                RecordedEvents.Add(new DefaultEnvironmentCreated(_identifier));
            else
                RecordedEvents.Add(new EnvironmentCreated(_identifier));

            return this;
        }

        /// <summary>
        ///     create Events that Delete this DomainObject when saved
        /// </summary>
        /// <returns></returns>
        public ConfigEnvironment Delete()
        {
            if (!_isDefault)
                RecordedEvents.Add(new EnvironmentDeleted(_identifier));

            return this;
        }

        /// <summary>
        ///     create Events that Modify this DomainObjects Keys' when saved
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public ConfigEnvironment ModifyKeys(IEnumerable<ConfigKeyAction> actions)
        {
            RecordedEvents.Add(new EnvironmentKeysModified(_identifier, actions.ToArray()));

            return this;
        }
    }
}