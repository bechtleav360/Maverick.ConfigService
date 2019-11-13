﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.DomainObjects;

namespace Bechtle.A365.ConfigService.Interfaces.Stores
{
    // @TODO: each new instance of ISnapshotStore has to be explicitly used by SnapshotService
    /// <summary>
    ///     Store, to retrieve Snapshots of <see cref="StreamedObject"/>
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        ///     get the latest snapshot - if possible - of the given StreamedObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetSnapshot<T>(string identifier) where T : StreamedObject;

        /// <summary>
        ///     get the latest snapshot - if possible - of the given StreamedObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetSnapshot<T>(string identifier, long maxVersion) where T : StreamedObject;

        /// <summary>
        ///     get the latest snapshot - if possible - with the given parameters
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetSnapshot(string dataType, string identifier);

        /// <summary>
        ///     get the latest snapshot - if possible - with the given parameters
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="identifier"></param>
        /// <param name="maxVersion"></param>
        /// <returns></returns>
        Task<IResult<StreamedObjectSnapshot>> GetSnapshot(string dataType, string identifier, long maxVersion);

        /// <summary>
        ///     save the given <see cref="StreamedObjectSnapshot"/> to the configured Store
        /// </summary>
        /// <param name="snapshots"></param>
        /// <returns></returns>
        Task<IResult> SaveSnapshots(IList<StreamedObjectSnapshot> snapshots);

        /// <summary>
        ///     get the highest event-number of the currently saved snapshots
        /// </summary>
        /// <returns></returns>
        Task<IResult<long>> GetLatestSnapshotNumbers();
    }
}