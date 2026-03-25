using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polaris.Domain.Interfaces.IRepositories;
using Polaris.Infrastructure.Data;
using Polaris.Infrastructure.Identity;
using System.Collections;

namespace Polaris.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation.
    /// Coordinates all repository instances and database transactions,
    /// ensuring that all changes within a single business operation
    /// are committed or rolled back atomically.
    ///
    /// Note on GenerationJobRepository:
    ///   The GenerationJobProcessor (Hangfire background worker) uses
    ///   IGenerationJobRepository directly — not through UoW — because
    ///   it operates in a long-running background context where each
    ///   incremental save must be committed independently. This is an
    ///   intentional and documented exception to the UoW pattern.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private Hashtable? _repositories;

        // ─── Identity Managers (needed by AuthRepository) ─────────────────
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // ─── Lazy-initialized Repository Fields ───────────────────────────
        private IUserRepository? _users;
        private IAuthRepository? _auth;
        private IGenerationJobRepository? _generationJobs;
        private IConversationRepository? _conversations;
        private IMessageRepository? _messages;

        public UnitOfWork(
            AppDbContext context,
            IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        // ─── Repository Properties (lazy initialization) ──────────────────

        public IUserRepository Users =>
            _users ??= new UserRepository(_context);

        public IAuthRepository Auth =>
            _auth ??= new AuthRepository(_userManager, _signInManager, _roleManager);

        public IGenerationJobRepository GenerationJobs
        {
            get
            {
                if (_generationJobs == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<GenerationJobRepository>>();
                    _generationJobs = new GenerationJobRepository(_context, logger);
                }
                return _generationJobs;
            }
        }

        public IConversationRepository Conversations
        {
            get
            {
                if (_conversations == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<ConversationRepository>>();
                    _conversations = new ConversationRepository(_context, logger);
                }
                return _conversations;
            }
        }

        public IMessageRepository Messages
        {
            get
            {
                if (_messages == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<MessageRepository>>();
                    _messages = new MessageRepository(_context, logger);
                }
                return _messages;
            }
        }

        // ─── Generic Repository Factory ───────────────────────────────────

        public IGenericRepository<T> GetGenericRepository<T>() where T : class
        {
            _repositories ??= new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(
                    repositoryType.MakeGenericType(typeof(T)), _context);

                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }

        // ─── Transaction Management ───────────────────────────────────────

        public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                T result = await action();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                }
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}