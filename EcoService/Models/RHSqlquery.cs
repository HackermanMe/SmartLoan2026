using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;

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
        public void InsertLoans(string numeroCompte, string reference, string type, string montantEmprunte, string enCours, float taux, string mensualites, DateTime dateDebut, DateTime dateFin)
        {
            string query = "INSERT INTO RHPretsExistants (" +
                "NumeroCompte, ReferencePret, TypeCredit, Montant, EnCours, Taux, Mensualites, StartDate, EndDate)" +
                " VALUES (@NumeroCompte, @ReferencePret, @TypeCredit, @Montant, @EnCours, @Taux, @Mensualites, @StartDate, @EndDate)";
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
            });
        }

        // Méthode pour récupérer un ou des prêts selon le matricule du staff
        [HttpPost]
        public SqlDataReader PretExistants(int matricule)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites " +
                " FROM RHPretsExistants p " +
                "JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte WHERE s.Matricule = @Matricule ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        // Méthode pour récupérer les prêts selon le numéro de compte
        public SqlDataReader PretExistantsStaff(string numeroCompte)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites " +
                " FROM RHPretsExistants p JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte " +
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
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte";
            return ExecuteReader(query);
        }

        // Méthode pour récupérer un compte selon le matricule
        public SqlDataReader Account(int matricule)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a " +
                "JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte WHERE b.Matricule = @Matricule";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        // Méthode pour récupérer les informations du staff connecté selon le login(Nom d'utilisateur)
        public SqlDataReader AccountLogin(string login)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
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

        // Méthode pour supprimer un prêt autres banques
        

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

        // Méthode pour ajouter une demande de prêt à la base de données
        public async Task<int> CreerDemande(Demande demande)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO LoanRequests (Montant, TypePret, Taux, NbreEcheances, Status, Quotity, Matricule, CreatedAt, UpdatedAt) OUTPUT INSERTED.Id VALUES (@Montant, @Amount, @Status, @CreatedAt, @UpdatedAt)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Montant", demande.Montant);
                cmd.Parameters.AddWithValue("@TypePret", demande.TypePret);
                cmd.Parameters.AddWithValue("@Taux", demande.Taux);
                cmd.Parameters.AddWithValue("@NbreEcheances", demande.NbreEcheances);
                cmd.Parameters.AddWithValue("@Status", demande.Status);
                cmd.Parameters.AddWithValue("@Quotity", demande.Quotity);
                cmd.Parameters.AddWithValue("@Matricule", demande.Matricule);
                cmd.Parameters.AddWithValue("@CreatedAt", demande.CreatedAt);
                cmd.Parameters.AddWithValue("@UpdatedAt", demande.UpdatedAt);

                await conn.OpenAsync();
                int newId = (int)await cmd.ExecuteScalarAsync();
                return newId;
            }
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
    }
}
