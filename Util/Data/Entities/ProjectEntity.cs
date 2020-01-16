/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using FluentNHibernate.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Util.Extensions;
using Util.Logging;

namespace Util.Data.Entities
{
	public enum ProjectEntityLanguage
	{
		Java, PHP, C, CSharp
	}

	/// <summary>
	/// Represents an entity from the 'projects' table.
	/// </summary>
	public class ProjectEntity
	{
		public virtual UInt32 AiId { get; set; }
		public virtual UInt32 InternalId { get; set; }
		public virtual String Name { get; set; }
		public virtual ProjectEntityLanguage Language { get; set; }
		public virtual String CloneUrl { get; set; }
		public virtual Boolean WasCorrected { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		private Lazy<NameAndOwner> lazyProjectNameAndOwner;
		/// <summary>
		/// Returns the name of the Project's owner. This should work if the project's
		/// <see cref="CloneUrl"/> points to a URL that that has the name of the owner
		/// as 2nd to last part in it (where parts are separated by forward slashes).
		/// This is true for e.g. Github-style clone-Urls. You may check the property
		/// <see cref="HasProjectOwner"/> before accessing this property.
		/// </summary>
		public virtual String Owner => this.lazyProjectNameAndOwner.Value.Owner;

		/// <summary>
		/// Returns true if it was supposedly possible to extract a name of the project's
		/// owner from its clone-URL.
		/// </summary>
		public virtual Boolean HasProjectOwner => Owner != null;

		public ProjectEntity()
		{
			this.lazyProjectNameAndOwner = new Lazy<NameAndOwner>(() =>
			{
				var nao = new NameAndOwner();
				try
				{
					// For e.g. Github: https://github.com/EXL/AstroSmash.git
					var sp = this.CloneUrl.Split('/').Reverse().ToArray();

					nao.Name = sp.Length > 1 ? (sp[0].Contains(".git") ? sp[0].Substring(0, sp[0].IndexOf(".git")) : sp[0]) : null;
					nao.Owner = sp.Length > 1 ? sp[1] : null;
				} catch { }

				return nao;
			});
		}

		/// <summary>
		/// Cleans up <see cref="ProjectEntity"/>s in the database and attempts to probe
		/// and repair the git clone-URL. Checks and modifies <see cref="ProjectEntity.WasCorrected"/> as the status of an entity. Some entities
		/// have empty clone-URLs and cannot be fixed; those will be deleted, if required.
		/// </summary>
		/// <param name="deleteUselessEntities">If true, will delete such entities,
		/// that do not have a clone-URL. This method was primarily made to fix broken
		/// URLs. Entities without a repairable URL or any URL at all were considered
		/// useless.</param>
		public static void CleanUpDatabase(BaseLogger<ProjectEntity> logger, bool deleteUselessEntities = false, ExecutionPolicy execPolicy = ExecutionPolicy.Parallel)
		{
			using (var tempSess = DataFactory.Instance.OpenSession())
			{
				logger.LogDebug("Successfully probed the configured database.");


				var ps = tempSess.QueryOver<Data.Entities.ProjectEntity>().Where(p => !p.WasCorrected).Future();
				var toSave = new ConcurrentBag<ProjectEntity>();
				var toDelete = new ConcurrentBag<ProjectEntity>();
				var parallelOptions = new ParallelOptions();
				if (execPolicy == ExecutionPolicy.Linear)
				{
					parallelOptions.MaxDegreeOfParallelism = 1;
				}

				Parallel.ForEach(ps, parallelOptions, proj =>
				{
					if (String.IsNullOrEmpty(proj.CloneUrl))
					{
						logger.LogDebug("Entity with ID {0} has an empty clone-URL.", proj.AiId);
						toDelete.Add(proj);
						return;
					}

					String realUrl = null;
					try
					{
						realUrl = proj.CloneUrl.Substring(0, proj.CloneUrl.LastIndexOf('/')) + "/" + proj.Name + ".git";
					}
					catch (Exception e)
					{
						logger.LogError(e, e.Message);
					}

					if (realUrl == proj.CloneUrl)
					{
						proj.WasCorrected = true;
						toSave.Add(proj);
						return;
					}

					using (var wc = new HttpClient())
					{
						var req = new HttpRequestMessage(HttpMethod.Head, realUrl);
						var task = wc.SendAsync(req);
						task.Wait();
						if (task.Result.StatusCode == System.Net.HttpStatusCode.OK)
						{
							logger.LogDebug("OK: {0}", realUrl);
							// We should update the URL:
							proj.CloneUrl = realUrl;
							proj.WasCorrected = true;
							toSave.Add(proj);
						}
						else if (task.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
						{
							logger.LogError("NOT FOUND: {0}", realUrl);
							// Usually happens when the repository disappears.
							toDelete.Add(proj);
						}
						else
						{
							logger.LogError("ERROR: {0}", realUrl);
						}
					}
				});


				if (deleteUselessEntities)
				{
					using (var trans = tempSess.BeginTransaction())
					{
						foreach (var item in toDelete)
						{
							tempSess.Delete(item);
						}
						trans.Commit();
						logger.LogInformation("Cleaned up {0} broken items.", toDelete.Count);
					}
				}


				foreach (var part in toSave.Partition(200))
				{
					using (var trans = tempSess.BeginTransaction())
					{
						foreach (var item in part)
						{
							tempSess.Update(item);
						}
						trans.Commit();
						logger.LogInformation("Repaired {0} items and stored them successfully.", part.Count);
					}
				}
			}
		}

		/// <summary>
		/// Create a new <see cref="ProjectEntity"/> from a clone-URL. Tries to determine
		/// the project's owner and name from the clone-URL.
		/// </summary>
		/// <param name="cloneUrl"></param>
		/// <returns></returns>
		public static ProjectEntity FromCloneUrl(string cloneUrl)
		{
			var pe = new ProjectEntity { CloneUrl = cloneUrl };

			return new ProjectEntity
			{
				CloneUrl = cloneUrl,
				Name = pe.lazyProjectNameAndOwner.Value.Name
			};
		}

		private class NameAndOwner
		{
			public String Name { get; set; }
			public String Owner { get; set; }
		}
	}

	/// <summary>
	/// Maps the entity-class <see cref="ProjectEntity"/>.
	/// </summary>
	public class ProjectEntityMap : ClassMap<ProjectEntity>
	{
		public ProjectEntityMap()
		{
			this.Table("projects");
			this.Id(x => x.AiId).Column("AI_ID");
			this.Map(x => x.InternalId).Column("INTERNAL_ID").Index("IDX_INTERNAL_ID");
			this.Map(x => x.Name).Column("NAME");
			this.Map(x => x.Language).Column("LANGUAGE")
				.Index("IDX_LANGUAGE")
				.CustomType<StringEnumMapper<ProjectEntityLanguage>>();
			this.Map(x => x.CloneUrl).Column("CLONE_URL");
			this.Map(x => x.WasCorrected).Column("WAS_CORRECTED");

			this.HasOne<RepositoryEntity>(x => x.Repository).Cascade.Lock();
		}
	}
}
