using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Office.Word;
//using Microsoft.Office.Core;
using NLog;

namespace EcoService.Models
{
    public class RHSqlQuery
    {
        private string connectionString = WebConfigurationManager.ConnectionStrings["SqlDbconnexion"].ConnectionString;

        // Méthode pour exécuter une commande non requête (INSERT, DELETE, UPDATE)
        private void ExecuteNonQuery(string query, Action<SqlCommand> parameterizeCommand = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                parameterizeCommand?.Invoke(command);

                // Log avant l'ouverture de la connexion
                LogManager.GetCurrentClassLogger().Info($"Tentative de connexion à la base de données avec la requête : {command.CommandText}");

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // Méthode pour exécuter une requête et lire les résultats
        private SqlDataReader ExecuteReader(string query, Action<SqlCommand> parameterizeCommand = null)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand command = new SqlCommand(query, connection);
            parameterizeCommand?.Invoke(command);
            connection.Open();
            return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        // Méthode pour ajouter un utilisateur 
        public void InsertUser(User user)
        {
            string email = user.Email;
            var account = email.Split('@')[0];

            string query = "INSERT INTO Accounts (Nom, Prenom, Login, IDGroup, ProfilUser) " +
                "VALUES (@Nom, @Prenom, @Login, @IDGroup, @ProfilUser)";
            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@Nom", user.Nom);
                cmd.Parameters.AddWithValue("@Prenom", user.Prenom);
                cmd.Parameters.AddWithValue("@Login", account);
                cmd.Parameters.AddWithValue("@IDGroup", user.IdRole);
                cmd.Parameters.AddWithValue("@ProfilUser", user.Status);
            });
        }

        /// Méthode pour lister les rôles
        public List<RHRole> Role()
        {
            List<RHRole> roles = new List<RHRole>();
            string query = "SELECT idUser, NumeroCompte FROM RHAccounts";
            using (SqlDataReader reader = ExecuteReader(query))
            {
                while (reader.Read())
                {
                    roles.Add(new RHRole
                    {
                        Iduser = reader.GetInt32(reader.GetOrdinal("idUser")),
                        NumeroCompte = reader.GetString(reader.GetOrdinal("NumeroCompte"))
                    });
                }
            }
            return roles;
        }

        // Méthode pour supprimer les prêts existants de la base
        public void DeleteRHLoans()
        {
            string query = "DELETE FROM RHPretsExistants";
            ExecuteNonQuery(query);
        }

        // Méthode pour insérer des prêts existants dans la base
        public void InsertLoans(string numeroCompte, string reference, string type, decimal montantEmprunte, decimal enCours, float taux, decimal mensualites, DateTime dateDebut, DateTime dateFin)
        {
            string query = "INSERT INTO RHPretsExistants (" +
                "NumeroCompte, ReferencePret, TypeCredit, Montant, EnCours, Taux, Mensualites, StartDate, EndDate, CreatedAt)" +
                " VALUES (@NumeroCompte, @ReferencePret, @TypeCredit, @Montant, @EnCours, @Taux, @Mensualites, @StartDate, @EndDate, @CreatedAt)";
            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
                cmd.Parameters.AddWithValue("@ReferencePret", reference);
                cmd.Parameters.AddWithValue("@TypeCredit", type);
                cmd.Parameters.AddWithValue("@Montant", montantEmprunte);
                cmd.Parameters.AddWithValue("@EnCours", enCours);
                cmd.Parameters.AddWithValue("@Taux", taux);
                cmd.Parameters.AddWithValue("@Mensualites", mensualites);
                cmd.Parameters.AddWithValue("@StartDate", dateDebut);
                cmd.Parameters.AddWithValue("@EndDate", dateFin);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            });
        }

        // Méthode pour récupérer un ou des prêts selon le matricule du staff
        public SqlDataReader PretExistants(int matricule)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites, p.CreatedAt " +
                " FROM RHPretsExistants p " +
                "JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte WHERE s.Matricule = @Matricule ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        // Méthode pour récupérer les prêts selon le numéro de compte
        public SqlDataReader PretExistantsStaff(string numeroCompte)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites, p.CreatedAt " +
                " FROM RHPretsExistants p JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte " +
                " WHERE p.NumeroCompte = @NumeroCompte ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte));
        }

        // Méthode pour récupérer les prêts autres banques selon le numéro de compte
        public SqlDataReader AutresPretExistantsStaff(string numeroCompte)
        {
            string query = "SELECT p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites, p.CreatedAt " +
                " FROM RHAutresPretsExistants p JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte " +
                " WHERE p.NumeroCompte = @NumeroCompte ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte));
        }

        // Méthode pou récupérer un staff selon le matricule
        public SqlDataReader GetStaff(int matricule)
        {
            string query = "SELECT SalaireNet FROM RHStaffs WHERE Matricule = @Matricule";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        // Méthode pour récupérer les comptes
        public SqlDataReader Accounts(string login)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.IDGroup AS IDGroupe, a.NumeroCompte AS NumeroComptee, b.SalaireNet*500 AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte";
            return ExecuteReader(query);
        }

        // Méthode pour récupérer les comptes
        public SqlDataReader Accounts()
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet*500 AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte";
            return ExecuteReader(query);
        }

        // Méthode pour récupérer un compte selon le matricule
        public SqlDataReader Account(int matricule)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet*500 AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a " +
                "JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte WHERE b.Matricule = @Matricule";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        // Méthode pour récupérer les informations du staff connecté selon le login(Nom d'utilisateur)
        public SqlDataReader AccountLogin(string login)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet*500 AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte WHERE a.Login = @Login";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Login", login));
        }

        // Méthode pour récupérerles informations d'un compte selon le groupe
        public SqlDataReader AccountRole(string login)
        {
            string query = "SELECT b.IDGroup, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHRoles] b ON a.IDGroup = b.IDGroup WHERE a.Login = @Login";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Login", login));
        }

        public SqlDataReader Rapport(int id)
        {
            string query = "SELECT NumeroCompte, action, controller, nom FROM RHAccount WHERE NumeroCompte = @NumeroCompte";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@NumeroCompte", id));
        }

        // Méthode pour supprimer les staffs de la base de données 
        public void DeleteRHStaff()
        {
            string query = "DELETE FROM RHStaffs";
            ExecuteNonQuery(query);
        }        

        // Méthode pour insérer les informations des staffs dans la base de données 
        public void InsertStaffs(string matricule, string email, string salaireNet, string numeroCompte)
        {
            string query = "INSERT INTO RHStaffs (Matricule, Email, SalaireNet, NumeroCompte)" +
                " VALUES (@Matricule, @Email, @SalaireNet, @NumeroCompte)";
            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@Matricule", matricule);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@SalaireNet", salaireNet);
                cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
            });
        }

        // Méthode pour récupérer un ou des prêts par l'id
        public SqlDataReader GetLoanById(int id)
        {
            string query = "SELECT * FROM RHPretsExistants WHERE PretId = @PretId";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@PretId", id));
        }

        // Méthode pour rechercher un staff selon le matricule, le numéro de compte, le nom ou le prénom 
        public List<Dictionary<string, object>> SearchStaff(string searchTerm)
        {
            var staffList = new List<Dictionary<string, object>>();
            string query = "SELECT b.Matricule, a.Nom, a.Prenom, a.NumeroCompte FROM RHAccounts a " +
                "JOIN RHStaffs b ON a.NumeroCompte = b.NumeroCompte WHERE b.Matricule LIKE @SearchTerm OR a.Nom LIKE @SearchTerm " +
                "OR a.NumeroCompte LIKE @SearchTerm OR a.Prenom LIKE @SearchTerm";
            using (SqlDataReader reader = ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%")))
            {
                while (reader.Read())
                {
                    var staff = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        staff[reader.GetName(i)] = reader.GetValue(i);
                    }
                    staffList.Add(staff);
                }
            }
            return staffList;
        }

        // Méthode pour récupérer les prêts autres banques
        public List<Dictionary<string, object>> GetExistingLoans(string accountNumber)
        {
            var loansList = new List<Dictionary<string, object>>();
            string query = "SELECT TypeDeCredit, StartDate, Montant, EnCours, Mensualites, EndDate, NomBanque " +
                           "FROM RHAutresPretsExistants WHERE NumeroCompte = @AccountNumber";

            using (SqlDataReader reader = ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@AccountNumber", accountNumber)))
            {
                while (reader.Read())
                {
                    var loan = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        loan[reader.GetName(i)] = reader.GetValue(i);
                    }
                    loansList.Add(loan);
                }
            }
            return loansList;
        }

        // Méthode pour récupérer les prêts autres banques
        public List<Dictionary<string, object>> GetExistingLoansWithID(string accountNumber)
        {
            var loansList = new List<Dictionary<string, object>>();
            string query = "SELECT APretId, TypeDeCredit, StartDate, Montant, EnCours, Mensualites, EndDate, NomBanque " +
                           "FROM RHAutresPretsExistants WHERE NumeroCompte = @AccountNumber";

            using (SqlDataReader reader = ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@AccountNumber", accountNumber)))
            {
                while (reader.Read())
                {
                    var loan = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        loan[reader.GetName(i)] = reader.GetValue(i);
                    }
                    loansList.Add(loan);
                }
            }
            return loansList;
        }

        // Méthode pour supprimer les prêts existants autres banques selon l'id
        public bool SupprimerPretExistant(int pretId)
        {
            string query = "DELETE FROM RHAutresPretsExistants WHERE APretId = @PretId";

            try
            {
                ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@PretId", pretId); // Ajout du paramètre à la commande
                });

                return true; // Si la commande s'exécute sans erreur, on retourne true
            }
            catch (Exception ex)
            {
                return false; // En cas d'erreur, on retourne false
            }
        }


        // Méthode pour rechercher un compte staff selon le matricule, le numéro de compte, le nom ou le prénom 
        public List<Dictionary<string, object>> SearchAccounts(string searchTerm)
        {
            var staffList = new List<Dictionary<string, object>>();
            string query = "SELECT b.Matricule, a.Nom, a.Prenom, a.IDGroup FROM RHAccounts a " +
                "JOIN RHStaffs b ON a.NumeroCompte = b.NumeroCompte WHERE b.Matricule LIKE @SearchTerm OR a.Nom LIKE @SearchTerm " +
                "OR a.NumeroCompte LIKE @SearchTerm OR a.Prenom LIKE @SearchTerm";
            using (SqlDataReader reader = ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%")))
            {
                while (reader.Read())
                {
                    var staff = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        staff[reader.GetName(i)] = reader.GetValue(i);
                    }
                    staffList.Add(staff);
                }
            }
            return staffList;
        }


        // Méthode pour récupérer les prêts CARPLAN
        public SqlDataReader GetCarPlans()
        {
            string query = "SELECT * FROM RHPretsExistants WHERE ReferencePret LIKE 'M61ACAS%' OR ReferencePret LIKE 'M61CASA%';";
            return ExecuteReader(query);
        }

        // Méthode pour récupérer une demande de prêt selon le matricule
        public async Task<Demande> GetLoanRequest(int matricule)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM RHDemandes WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Matricule", matricule);

                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    return new Demande
                    {
                        DemandeId = (int)reader["DemandeId"],
                        Montant = (decimal)reader["Montant"],
                        TypePret = (string)reader["TypePret"],
                        Taux = (float)reader["Taux"],
                        NbreEcheances = (int)reader["NbreEcheances"],
                        Status = (string)reader["Status"],
                        Quotity = (float)reader["Quotity"],
                        Matricule = (int)reader["Matricule"],
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        UpdatedAt = (DateTime)reader["UpdatedAt"]
                    };
                }
                return null;
            }
        }

        // Méthode pour envoyer les simulations
        public void SendSimulation(decimal MontantEmprunte, string TypePret, decimal annualRate, int months, decimal netSalary, int matricule)
        {
          
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("INSERT INTO RHDemandes (Montant, TypePret, Taux, NbreEcheances, SalaireNet, Matricule, CreatedAt) VALUES (@Montant, @TypePret, @Taux, @NbreEcheances, @SalaireNet, @Matricule, @CreatedAt)", connection);

                command.Parameters.AddWithValue("@Montant", MontantEmprunte);
                command.Parameters.AddWithValue("@TypePret", TypePret);
                command.Parameters.AddWithValue("@Taux", annualRate);
                command.Parameters.AddWithValue("@NbreEcheances", months);
                command.Parameters.AddWithValue("@SalaireNet", netSalary);
                command.Parameters.AddWithValue("@Matricule", matricule);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                
                command.ExecuteNonQuery();
            }
        }

        // Méthode pour mettre à jour la table RHDemandes
        public void UpdateDemandes(string nomComplet, string numeroCompte, DateTime dateNaissance)
        {

        }

        // Méthode pour mettre à jour une simulation
        public void UpdateSimulation(int matricule, string nomPrenoms, string numeroCompte, DateTime dateNaissance)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand())
                {
                    // Associer la connexion à la commande
                    command.Connection = connection;

                    // Requête SQL pour mettre à jour les informations dans la table
                    command.CommandText = "UPDATE RHDemandes " +
                                             "SET NomPrenoms = @NomPrenoms, NumeroCompte = @NumeroCompte, DateNaissance = @DateNaissance, UpdatedAt = @UpdatedAt " +
                                             "WHERE Matricule = @Matricule";

                    // Ajout des paramètres
                    command.Parameters.AddWithValue("@NomPrenoms", nomPrenoms);
                    command.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
                    command.Parameters.AddWithValue("@DateNaissance", dateNaissance);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@Matricule", matricule);

                    // Exécuter la commande
                    command.ExecuteNonQuery();
                }
            }
        }


        // Méthode pour ajouter les Prets existant autres banques
        public void InsertAutresPretsExistants(string TypePret, string nomBanque, DateTime StartDate, DateTime EndDate, decimal Montant, decimal Mensualites, decimal EnCours, string numeroCompte)
        {
           
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var command = new SqlCommand("INSERT INTO RHAutresPretsExistants (TypeDeCredit, NomBanque, StartDate, EndDate, Montant, Mensualites, EnCours, NumeroCompte) VALUES (@TypeDeCredit, @NomBanque, @StartDate, @EndDate, @Montant, @Mensualites, @EnCours, @NumeroCompte)", connection);

                command.Parameters.AddWithValue("@TypeDeCredit", TypePret);
                command.Parameters.AddWithValue("@NomBanque", nomBanque);
                command.Parameters.AddWithValue("@StartDate", StartDate);
                command.Parameters.AddWithValue("@EndDate", EndDate);
                command.Parameters.AddWithValue("@Montant", Montant);
                command.Parameters.AddWithValue("@Mensualites", Mensualites);
                command.Parameters.AddWithValue("@EnCours", EnCours);
                command.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
                
                command.ExecuteNonQuery();
            }
        }

        // Méthode pour récupérer la liste des demandes de prêt
        public async Task<List<Demande>> GetLoanRequests()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM RHDemandes";
                SqlCommand cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                List<Demande> demandes = new List<Demande>();
                while (reader.Read())
                {
                    demandes.Add(new Demande
                    {
                        DemandeId = (int)reader["DemandeId"],
                        Montant = (decimal)reader["Montant"],
                        TypePret = (string)reader["TypePret"],
                        Taux = (float)reader["Taux"],
                        NbreEcheances = (int)reader["NbreEcheances"],
                        Status = (string)reader["Status"],
                        Quotity = (float)reader["Quotity"],
                        Matricule = (int)reader["Matricule"],
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        UpdatedAt = (DateTime)reader["UpdatedAt"]
                    });
                }
                return demandes;
            }
        }

        // Méthode pour approuver les demandes de Prêts (changer le status 'Pending' en 'Approuvé')
        public async Task<bool> ApproveLoanRequest(int matricule)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE RHDemandes SET Status = 'Approuvé', UpdatedAt = @UpdatedAt WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Matricule", matricule);
                cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                await conn.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Méthode pour autoriser l'accès à un utilisateur 
        public async Task<bool> ApproveUser(int matricule) {
            using (SqlConnection connection = new SqlConnection(connectionString)) {
                string query = "UPDATE RHAccounts SET IDGroup = 2 WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand (query, connection);
                cmd.Parameters.AddWithValue("@Matricule", matricule);

                await connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Méthode pour supprimer l'accès à un utilisateur 
        public async Task<bool> RevokeUser(int matricule)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE RHAccounts SET IDGroup = 1 WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Matricule", matricule);

                await connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Méthode pour autoriser l'accès à l'administration staff 
        public async Task<bool> ApproveStaffAdmin(int matricule)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE RHAccounts SET IDGroup = 100 WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Matricule", matricule);

                await connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Méthode pour autoriser l'accès à l'administration des comptes
        public async Task<bool> ApproveAccountsAdmin(int matricule)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE RHAccounts SET IDGroup = 101 WHERE Matricule = @Matricule";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Matricule", matricule);

                await connection.OpenAsync();
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Méthode pour changer le rôle de l'utilisateur 
        public bool UpdateUserRole(int matricule, int role)
        {
            string query = "UPDATE RHAccounts SET IDGroup = @IDGroup WHERE Matricule = @Matricule";
            
            try
            {
                ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Matricule", matricule);
                    cmd.Parameters.AddWithValue("@IDGroup", role);
                });

                return true; // Si la commande s'exécute sans erreur, on retourne true
            }
            catch (Exception ex)
            {
                // Log de l'exception
                LogManager.GetCurrentClassLogger().Error(ex, "Erreur lors de la mise à jour du rôle de l'utilisateur.");
                return false; // Retourne false en cas d'échec
            }
        }

        // Méthode pour proposer un attribution de rôle
        public bool ProposeUserRoleChange(int matricule, int newRole, int proposedBy)
        {
            string query = "INSERT INTO RHRoleChangesPending (Matricule, NewRole, ProposedBy) VALUES (@Matricule, @NewRole, @ProposedBy)";

            try
            {
                ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Matricule", matricule);
                    cmd.Parameters.AddWithValue("@NewRole", newRole);
                    cmd.Parameters.AddWithValue("@ProposedBy", proposedBy);
                });

                return true; // Si la proposition s'exécute sans erreur, on retourne true
            }
            catch (Exception ex)
            {
                // Log de l'exception
                LogManager.GetCurrentClassLogger().Error(ex, "Erreur lors de la proposition de changement de rôle.");
                return false; // Retourne false en cas d'échec
            }
        }

        // Méthode pour récupérer la liste des demandes de prêt
        public List<RHDemande> GetDemandesPret()
        {
            string query = "SELECT Montant, TypePret, Taux, NbreEcheances, Matricule, CreatedAt, SalaireNet, NomPrenoms, NumeroCompte, DateNaissance FROM RHDemandes";
            List<RHDemande> demandes = new List<RHDemande>();

            SqlDataReader reader = ExecuteReader(query, cmd => { /* Pas de paramètres ici */ });

            while (reader.Read())
            {
                RHDemande demande = new RHDemande
                {
                    Montant = reader.GetDecimal(0),
                    TypePret = reader.GetString(1),
                    Taux = reader.GetDouble(2),
                    NbreEcheances = reader.GetInt32(3),
                    Matricule = reader.GetInt32(4),
                    CreatedAt = reader.GetDateTime(5),
                    SalaireNet = reader.GetDecimal(6),
                    NomComplet = reader.GetString(7),
                    NumeroCompte = reader.GetString(8),
                    DateNaissance = reader.GetDateTime(9)
                };
                demandes.Add(demande);
            }

            return demandes;
        }


        // Méthode pour obtenir les changements de rôle en attente
        public List<RoleChangePendingModel> GetPendingRoleChanges()
        {
            List<RoleChangePendingModel> pendingChanges = new List<RoleChangePendingModel>();

            string query = "SELECT Id, Matricule, NewRole, ProposedBy, CreatedAt FROM RHRoleChangesPending WHERE Status = 'Pending'";

            try
            {
                SqlDataReader reader = ExecuteReader(query, cmd =>
                {
                    // Pas de paramètres à ajouter ici
                });

                while (reader.Read())
                {
                    RoleChangePendingModel change = new RoleChangePendingModel
                    {
                        Id = reader.GetInt32(0),
                        Matricule = reader.GetInt32(1),
                        NewRole = reader.GetInt32(2),
                        ProposedBy = reader.GetInt32(3),
                        CreatedAt = reader.GetDateTime(4)
                    };
                    // Optionnel : récupérer le nom complet de l'utilisateur et de celui qui propose
                    change.NomComplet = GetUserFullName(change.Matricule);
                    change.ProposePar = GetUserFullName(change.ProposedBy);
                    pendingChanges.Add(change);
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Erreur lors de la récupération des changements de rôle en attente.");
            }

            return pendingChanges;
        }

        // Méthode pour obtenir le nom complet d'un utilisateur à partir de son Matricule
        private string GetUserFullName(int matricule)
        {
            string fullName = "N/A";
            string query = "SELECT Nom, Prenom FROM RHAccounts WHERE Matricule = @Matricule";

            SqlDataReader reader = ExecuteReader(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@Matricule", matricule);
            });

            if (reader.Read())
            {
                fullName = reader["Nom"].ToString() + " " + reader["Prenom"].ToString();
            }
            
            return fullName;
        }


        // Méthode pour valider l'attribution de rôle 
        public bool ValidateUserRoleChange(int changeId, bool isApproved)
        {
            try
            {
                if (isApproved)
                {
                    // Récupérer la proposition
                    var querySelect = "SELECT Matricule, NewRole FROM RHRoleChangesPending WHERE Id = @Id";
                    var reader = ExecuteReader(querySelect, cmd => cmd.Parameters.AddWithValue("@Id", changeId));

                    if (reader.Read())
                    {
                        int matricule = reader.GetInt32(0);
                        int newRole = reader.GetInt32(1);

                        // Appliquer le changement de rôle
                        var queryUpdate = "UPDATE RHAccounts SET IDGroup = @NewRole WHERE Matricule = @Matricule";
                        ExecuteNonQuery(queryUpdate, cmd =>
                        {
                            cmd.Parameters.AddWithValue("@NewRole", newRole);
                            cmd.Parameters.AddWithValue("@Matricule", matricule);
                        });

                        // Mettre à jour le statut de la proposition
                        var queryStatus = "UPDATE RHRoleChangesPending SET Status = 'Approved' WHERE Id = @Id";
                        ExecuteNonQuery(queryStatus, cmd => cmd.Parameters.AddWithValue("@Id", changeId));
                    }
                }
                else
                {
                    // Rejet de la proposition
                    var queryStatus = "UPDATE RHRoleChangesPending SET Status = 'Rejected' WHERE Id = @Id";
                    ExecuteNonQuery(queryStatus, cmd => cmd.Parameters.AddWithValue("@Id", changeId));
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Erreur lors de la validation du changement de rôle.");
                return false;
            }
        }

        // Méthode pour créer un utilisateur
        public int CreateUser(int matricule, string nom, string prenom, string email, string numeroCompte, int roleId)
        {
            string query = "INSERT INTO RHAccounts (Matricule, Nom, Prenom, Email, NumeroCompte, IDGroup) OUTPUT INSERTED.IDUser VALUES (@Matricule, @Nom, @Prenom, @Email, @NumeroCompte, @RoleId)";
            int userId = 0;

            SqlDataReader reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Matricule", matricule);
                    cmd.Parameters.AddWithValue("@Nom", nom);
                    cmd.Parameters.AddWithValue("@Prenom", prenom);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                }
            );

            if (reader.Read())
            {
                userId = reader.GetInt32(0);
            }
            
            return userId;
        }

        // Méthode pour ajouter un changement de rôle en attente
        public void AddPendingRoleChange(int matricule, int userId, int newRoleId)
        {
            string query = "INSERT INTO RHRoleChangesPending (Matricule, NewRole, ProposedBy) VALUES (@Matricule, @NewRole, @ProposedBy)";

            ExecuteNonQuery(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@Matricule", matricule);
                cmd.Parameters.AddWithValue("@NewRole", newRoleId);
                cmd.Parameters.AddWithValue("@ProposedBy", userId);
            });
        }

        // Méthode qui récupère la liste des rôles 
        public List<SelectListItem> GetAllRoles()
        {
            var roles = new List<SelectListItem>();
            string query = "SELECT IDGroup, Libelle FROM RHRoles";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    roles.Add(new SelectListItem
                    {
                        Value = reader["IDGroup"].ToString(),
                        Text = reader["Libelle"].ToString()
                    });
                }
            }

            return roles;
        }



    }

}
