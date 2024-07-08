using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public void DeleteRHLoans()
        {
            string query = "DELETE FROM RHPretsExistants";
            ExecuteNonQuery(query);
        }

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

        [HttpPost]
        public SqlDataReader PretExistants(int id)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites " +
                " FROM RHPretsExistants p " +
                "JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte WHERE s.Matricule = @Matricule ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", id));
        }

        public SqlDataReader PretExistantsStaff(string id)
        {
            string query = "SELECT p.PretId, p.ReferencePret, p.NumeroCompte, p.Montant, p.EnCours, p.Taux, p.TypeCredit, p.StartDate, p.EndDate, p.Mensualites " +
                " FROM RHPretsExistants p JOIN RHStaffs s ON p.NumeroCompte = s.NumeroCompte " +
                " WHERE p.NumeroCompte = @NumeroCompte ORDER BY p.ReferencePret";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@NumeroCompte", id));
        }

        public SqlDataReader GetStaff(int id)
        {
            string query = "SELECT SalaireNet FROM RHStaffs WHERE Matricule = @Matricule";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", id));
        }

        public SqlDataReader Accounts(string login)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte";
            return ExecuteReader(query);
        }

        public SqlDataReader Account(int matricule)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a " +
                "JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte WHERE b.Matricule = @Matricule";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Matricule", matricule));
        }

        public SqlDataReader AccountLogin(string login)
        {
            string query = "SELECT b.Matricule AS Matriculee, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee, b.SalaireNet AS SalaireNete " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHStaffs] b ON a.NumeroCompte = b.NumeroCompte WHERE a.Login = @Login";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Login", login));
        }

        public SqlDataReader AccountRole(string login)
        {
            string query = "SELECT b.idGroup,  a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee " +
                "FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHRole] b ON a.idGroup = b.idGroup WHERE a.Login = @Login";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@Login", login));
        }

        public SqlDataReader Rapport(int id)
        {
            string query = "SELECT NumeroCompte, action, controller, nom FROM RHAccount WHERE NumeroCompte = @NumeroCompte";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@NumeroCompte", id));
        }

        public void DeleteRHStaff()
        {
            string query = "DELETE FROM RHStaffs";
            ExecuteNonQuery(query);
        }

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

        public SqlDataReader GetLoanById(int id)
        {
            string query = "SELECT * FROM RHPretsExistants WHERE PretId = @PretId";
            return ExecuteReader(query, cmd => cmd.Parameters.AddWithValue("@PretId", id));
        }

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
    }
}
