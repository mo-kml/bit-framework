﻿using Bit.Core.Contracts;
using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class UsersController : DtoController<UserDto>
    {
        public IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public IUserInformationProvider UserInformationProvider { get; set; }

        public IDtoModelMapper<UserDto, User> DtoModelMapper { get; set; }

        [Function]
        public async Task<SingleResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(UserInformationProvider.GetCurrentUserId());

            return SingleResult.Create(DtoModelMapper.FromModelQueryToDtoQuery((await UsersRepository.GetAllAsync(cancellationToken)))
                 .Where(u => u.Id == userId));
        }
    }
}