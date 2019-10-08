﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Rosetta.Models;

namespace Rosetta.Services
{
    public interface IRosettaStoneService
    {
        Task<Status> GetStatus();
        Task<RosettaFranchise> GetFranchise(int franchiseNumber);
        Task<IList<RosettaFranchise>> GetFranchises();
        Task<IList<RosettaAgency>> GetAgencies();
        void ClearCache();
    }
}