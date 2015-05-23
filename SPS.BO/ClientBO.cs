﻿using SPS.BO.Exceptions;
using SPS.Model;
using SPS.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SPS.BO
{
    public class MontlyClientBO : IBusiness<MonthlyClient, string>
    {
        public virtual void Add(MonthlyClient client)
        {
            using (var context = new SPSDb())
            {
                if (this.Find(client.CPF) != null)
                {
                    throw new UniqueKeyViolationException(string.Format("There is already a client with CPF {0}.", client.CPF));
                }

                if (client.Address != null)
                {
                    client.Address = context.Addresses.SingleOrDefault(a => a.PostalCode == client.Address.PostalCode);
                }

                context.Clients.Add(client);
                context.SaveChanges();
            }
        }

        public virtual MonthlyClient Find(string cpf)
        {
            MonthlyClient client = null;

            using (var context = new SPSDb())
            {
                client = context.Clients
                    .Include("Tags")
                    .Include("Records")
                    .Include("Parking")
                    .Include("Address")
                    .SingleOrDefault(c => c.CPF == cpf);
            }

            return client;
        }

        public virtual IList<MonthlyClient> FindAll()
        {
            using (var context = new SPSDb())
            {
                return context.Clients
                    .Include("Tags")
                    .Include("Records")
                    .Include("Parking")
                    .Include("Address")
                    .ToList();
            }
        }

        public virtual void Remove(MonthlyClient client)
        {
            using (var context = new SPSDb())
            {
                context.Clients.Remove(client);
                context.SaveChanges();
            }
        }

        public virtual void Update(MonthlyClient client)
        {
            using (var context = new SPSDb())
            {
                MonthlyClient entity;

                if (client.Id > 0)
                {
                    entity = context.Clients.Find(client.Id);
                }
                else
                {
                    entity = context.Clients.SingleOrDefault(c => c.Email == client.Email);
                }

                if (entity == null)
                    return;

                client.Id = entity.Id;
                context.Entry(entity).CurrentValues.SetValues(client);

                if (client.Address != null)
                {
                    entity.Address = context.Addresses.SingleOrDefault(a => a.PostalCode == client.Address.PostalCode);
                }

                if (client.Parking != null)
                {
                    entity.Parking = context.Parkings.Find(client.Parking.CNPJ);
                }

                if (client.Tags != null)
                {
                    entity.Tags = client.Tags;
                }

                context.SaveChanges();
            }
        }
    }
}

