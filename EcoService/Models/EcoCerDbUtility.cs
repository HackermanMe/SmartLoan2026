using System;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Web.Configuration;
using NPOI.SS.Formula.Functions;
using Ecoservice.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace EcoService.Models
{
    public class EcoCerDbUtility
    {
        //private readonly string? _connectionString;
        private string _connectionString;
        private readonly EcoCerLogger _logger;

        //public EcoCerDbUtility(IConfiguration configuration)
        //{
        //    //_connectionString = configuration.GetConnectionString("SqlDbconnexion");
        //    _connectionString = WebConfigurationManager.ConnectionStrings["SqlDbconnexion"].ConnectionString;
        //    _logger = new EcoCerLogger();
        //}
        //public EcoCerDbUtility() { } // Constructeur sans paramètre requis
        public EcoCerDbUtility()
        {
            _connectionString = WebConfigurationManager.ConnectionStrings["SqlDbconnexion"].ConnectionString;
            _logger = new EcoCerLogger();
        }

        public SqlDataReader ExecuteReader(string query, Action<SqlCommand> configureCommand)
        {
            var connection = new SqlConnection(_connectionString);
            var command = new SqlCommand(query, connection);

            configureCommand?.Invoke(command);

            connection.Open();
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public int ExecuteNonQuery(string query, Action<SqlCommand> configureCommand)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    configureCommand?.Invoke(command);
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }

            }
        }

        public object ExecuteScalar(string query, Action<SqlCommand> configureCommand)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    configureCommand?.Invoke(command);
                    connection.Open();
                    return command.ExecuteScalar();

                }
            }
        }

        //public static DateOnly GetDateOnly(IDataReader reader, string columnName)
        //{
        //    return reader.IsDBNull(reader.GetOrdinal(columnName))
        //        ? default(DateOnly)
        //        : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal(columnName)));
        //}

        public static DateTime? GetDateOnly(IDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName))
                ? (DateTime?)null
                : reader.GetDateTime(reader.GetOrdinal(columnName)).Date;
        }


        public EcoCerUser? GetUser(string login, string password)
        {
            try
            {
                string query = "SELECT TOP(1) UserId, Login, Role FROM EcoCerUsers WHERE Login = @Login AND Password = @Password";

                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", password);

                }))
                {
                    if (reader.Read())
                    {
                        return new EcoCerUser
                        {
                            UserId = Convert.ToInt32(reader["UserId"]),
                            Login = reader["Login"].ToString(),
                            Role = reader["Role"].ToString(),
                        };
                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when fetching user by username and password:", ex);
                throw new Exception("An error occured while fetching a user");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetUser", ex);
                throw;
            }
        }

        //public EcoCerUserDetails? GetUserByLogin(string login)
        //{
        //    try
        //    {
        //        //string query = "SELECT TOP(1) * FROM EcoCerUsers WHERE Login = @Login";
        //        string query = "SELECT TOP(1) IDUser,Login,Nom,Prenom FROM RHAccounts where login= @Login";

        //        using (var reader = ExecuteReader(query, cmd =>
        //        {
        //            cmd.Parameters.AddWithValue("@Login", login);
        //        }))
        //        {
        //            if (reader.Read())
        //            {
        //                return new EcoCerUserDetails
        //                {
        //                    Login = reader["Login"].ToString(),
        //                    Nom = reader["Nom"].ToString(),
        //                    Prenom = reader["Prenom"].ToString(),
        //                    //Sexe = reader["Sexe"].ToString(),
        //                    //Civilite = reader["Civilite"].ToString(),
        //                    //CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
        //                    //DateRecrutement = reader["DateRecrutement"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateRecrutement"]).Date
        //                };
        //            }
        //        }

        //        return null;
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError("Database error when fetching user by Login:", ex);
        //        throw new Exception("An error occured while fetching a user");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Unexpected error in GetUserByLogin", ex);
        //        throw;
        //    }
        //}

        //Requête de recherche d'un utilisateur
        public DataTable SearchUser(string param)
        {
            try
            {

                string query = @"SELECT TOP(1) r.IDUser,r.Login,r.Nom,r.Prenom, e.CategorieProfessionnelle, e.DateRecrutement FROM RHAccounts r 
                                JOIN EcoCerStaffInfos e 
                                ON r.Email = e.Email
                                where r.Login LIKE @Param OR r.Email LIKE @Param";

                DataTable searchedUser = new DataTable();

                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Param", "%" + param + "%");
                }))
                {
                    if (reader.HasRows)
                    {
                        searchedUser.Load(reader);
                        return searchedUser;
                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when searching user:", ex);
                throw new Exception("An error occured while searching a user");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in SearchUser", ex);
                throw;
            }
        }
        //public EcoCerCertificateDataModel? GetCertificateDataModel(string login)
        //{
        //    try
        //    {
        //        string query = "SELECT TOP(1) * FROM EcoCerCertificateDataModels WHERE UserLogin = @UserLogin";

        //        using (var reader = ExecuteReader(query, cmd =>
        //        {
        //            cmd.Parameters.AddWithValue("@UserLogin", login);
        //        }))
        //        {
        //            if (reader.Read())
        //            {
        //                return new EcoCerCertificateDataModel
        //                {
        //                    RefNumber = reader["RefNumber"].ToString(),
        //                    Login = reader["UserLogin"].ToString(),
        //                    Nom = reader["Nom"].ToString(),
        //                    Prenom = reader["Prenom"].ToString(),
        //                    Sexe = reader["Sexe"].ToString(),
        //                    Civilite = reader["Civilite"].ToString(),
        //                    CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
        //                    DateRecrutement = reader["DateRecrutement"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateRecrutement"]).Date,
        //                    CreationDate = reader["CreationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreationDate"]).Date

        //                };
        //            }
        //        }

        //        return null;
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError($"Database error when fetching CertificateDataModel:", ex);
        //        throw new Exception("An error occured while fetching certificate data");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Unexpected error in GetCertificateDataModel", ex);
        //        throw;
        //    }
        //}

        public EcoCerCertificateDataModel GetCertificateDataModell(string login)
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                string query = "SELECT TOP(1) * FROM EcoCerCertificateDataModels WHERE UserLogin = @UserLogin";
                //string query = "SELECT TOP(1) IDUser,Login,Nom,Prenom FROM RHAccounts where login= @UserLogin";
                connection.Open();
                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.Add(new SqlParameter("@UserLogin", SqlDbType.NVarChar) { Value = login });
                }))
                {
                    if (reader.Read())
                    {
                        return new EcoCerCertificateDataModel
                        {
                            RefNumber = reader["RefNumber"].ToString(),
                            //RefNumber = reader["IDUser"].ToString(),
                            Login = reader["UserLogin"].ToString(),
                            //Login = reader["Login"].ToString(),
                            Nom = reader["Nom"].ToString(),
                            Prenom = reader["Prenom"].ToString(),
                            Sexe = reader["Sexe"].ToString(),
                            Civilite = reader["Civilite"].ToString(),
                            CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                          //  DateRecrutement = reader["DateRecrutement"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateRecrutement"]).Date,
                            //CreationDate = reader["CreationDate"].ToString(),
                            CreationDate = reader["CreationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreationDate"]).Date
                        };
                    }
                }
                connection.Close();
                return null;
            }
            catch (SqlException ex)
            {
                if (_logger != null)
                {
                    _logger.LogError("Database error when fetching CertificateDataModel: " + ex.Message, ex);
                }
                throw new Exception("An error occurred while fetching certificate data", ex);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError("Unexpected error in GetCertificateDataModel: " + ex.Message, ex);
                }
                throw;
            }
        }

        public EcoCerCertificateDataModel? GetCertificateDataModel(string login)
        {
            try
            {
                string query = "SELECT TOP(1) CdmId,UserLogin,RefNumber,Nom,Prenom,Sexe,Civilite,CategorieProfessionnelle,DateRecrutement,CreationDate,CreationYear FROM EcoCerCertificateDataModels WHERE UserLogin = @UserLogin";
                //string query = "SELECT TOP(1) * FROM EcoCerCertificateDataModels WHERE UserLogin = @UserLogin";

                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserLogin", login);
                }))
                {
                    if (reader.Read())
                    {
                        return new EcoCerCertificateDataModel
                        {
                            CdmId = Convert.ToInt32(reader["CdmId"]),
                            Login = reader["UserLogin"].ToString(),
                            RefNumber = reader["RefNumber"].ToString(),
                            Nom = reader["Nom"].ToString(),
                            Prenom = reader["Prenom"].ToString(),
                            Sexe = reader["Sexe"].ToString(),
                            Civilite = reader["Civilite"].ToString(),
                            CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                           DateRecrutement = reader["DateRecrutement"].ToString(),
                            //DateRecrutement = GetDateOnly(reader, "DateRecrutement"),
                            //CreationDate = reader["CreationDate"].ToString(),
                            CreationDate = reader["CreationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreationDate"]).Date,
                            CreationYear = Convert.ToInt32(reader["CreationYear"])
                        };

                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError($"Database error when fetching CertificateDataModel:", ex);
                throw new Exception("An error occured while fetching certificate data");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetCertificateDataModel", ex);
                throw;
            }
        }

        public DataTable GetLastCertificateReferences(int year)
        {
            try
            {
                string query = @"SELECT TOP 1 CdmId, RefNumber, CreationDate, CreationYear FROM EcoCerCertificateDataModels 
                       WHERE CreationYear = @CreationYear ORDER BY RefNumber DESC;";
                DataTable lastCertificateReferences = new DataTable();

                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@CreationYear", year);
                }))
                {
                    if (reader.HasRows)
                    {
                        lastCertificateReferences.Load(reader);
                    }
                }

                return lastCertificateReferences;
            }
            catch (SqlException ex)
            {
                _logger.LogError($"Database error when fetching the last certificate:", ex);
                throw new Exception("An error occured while fetching the last certificate");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetLastCertificate", ex);
                throw;
            }
        }

        public EcoCerCertificateDataModel? GetCerCertificateDataModelById(int id)
        {
            string query = "SELECT TOP(1) * FROM EcoCerCertificateDataModels WHERE CdmId = @CdmId";

            using (var reader = ExecuteReader(query, cmd =>
            {
                cmd.Parameters.AddWithValue("@CdmId", id);
            }))
            {
                if (reader.Read())
                {
                    return new EcoCerCertificateDataModel
                    {
                        CdmId = Convert.ToInt32(reader["CdmId"]),
                        RefNumber = reader["RefNumber"].ToString(),
                        Login = reader["UserLogin"].ToString(),
                        Nom = reader["Nom"].ToString(),
                        Prenom = reader["Prenom"].ToString(),
                        Sexe = reader["Sexe"].ToString(),
                        Civilite = reader["Civilite"].ToString(),
                        CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                        DateRecrutement = reader["DateRecrutement"].ToString(),
                        //DateRecrutement = GetDateOnly(reader, "DateRecrutement"),
                        Statut = reader["Statut"].ToString(),
                        //CreationDate = reader["CreationDate"].ToString(),
                        CreationDate = reader["CreationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreationDate"]).Date,
                        CreationYear = Convert.ToInt32(reader["CreationYear"])
                    };
                }
            }

            return null;
        }

        public async Task<List<EcoCerCertificateDataModel>> GetCertificatesByStatus(string statut, string param)
        {
            try
            {
                string query = "";
                int year;
                int currentYear = DateTime.Now.Year;
                bool isNumeric = int.TryParse(param, out year);
                if (!string.IsNullOrEmpty(param))
                {
                    if (isNumeric)
                    {
                        /*Console.WriteLine("There's a numeric param: " + param);*/
                        //year = int.Parse(param);
                        //query = $"SELECT * FROM EcoCerCertificateDataModels WHERE Statut = '{statut}' AND CreationYear = '{year}' ORDER BY CreationDate DESC, RefNumber DESC";
                        query = $"SELECT CdmId,RefNumber,Nom,Prenom,Sexe,Civilite,CategorieProfessionnelle,DateRecrutement,Statut,CreationDate,CreationYear FROM EcoCerCertificateDataModels WHERE Statut = '{statut}' AND CreationYear = '{year}' ORDER BY CreationDate DESC, CAST(RefNumber AS INT) DESC";

                    }
                    else
                    {
                        /*Console.WriteLine("There's a param: " + param);*/
                        query = @$"SELECT * FROM EcoCerCertificateDataModels WHERE Statut = '{statut}' AND (RefNumber LIKE @SearchParam
                    OR Nom LIKE @SearchParam OR Prenom LIKE @SearchParam OR Sexe LIKE @SearchParam
                    OR Civilite LIKE @SearchParam OR CategorieProfessionnelle LIKE @SearchParam) ORDER BY CreationDate Desc, CAST(RefNumber AS INT) DESC";
                    }
                }
                else
                {
                    /*Console.WriteLine("No param was found");*/
                    query = $"SELECT * FROM EcoCerCertificateDataModels WHERE Statut = '{statut}' AND CreationYear = @Year ORDER BY CreationDate DESC, CAST(RefNumber AS INT) DESC";

                }

                var certificates = new List<EcoCerCertificateDataModel>();

                using (var reader = ExecuteReader(query, cmd =>
                {
                    if (!string.IsNullOrEmpty(param))
                    {
                        cmd.Parameters.AddWithValue("@SearchParam", "%" + param + "%");
                    }
                    cmd.Parameters.AddWithValue("@Year", currentYear);
                }))
                {

                    while (await reader.ReadAsync())
                    {
                        certificates.Add(new EcoCerCertificateDataModel
                        {
                            CdmId = Convert.ToInt32(reader["CdmId"]),
                            RefNumber = reader["RefNumber"].ToString(),
                            Nom = reader["Nom"].ToString(),
                            Prenom = reader["Prenom"].ToString(),
                            Sexe = reader["Sexe"].ToString(),
                            Civilite = reader["Civilite"].ToString(),
                            CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                            DateRecrutement = reader["DateRecrutement"].ToString(),
                            //DateRecrutement = GetDateOnly(reader, "DateRecrutement"),
                            Statut = reader["Statut"].ToString(),
                            //CreationDate = reader["CreationDate"].ToString(),
                            CreationDate = reader["CreationDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreationDate"]),
                            CreationYear = Convert.ToInt32(reader["CreationYear"])
                        });
                    }
                }

                return certificates;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when getting certificates by status:", ex);
                throw new Exception("An error occured while getting data in GetCertificatesByStatus");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetCertificatesByStatus", ex);
                throw;
            }
        }

        //Verifier si le login exist dans la table EcoCerUsers
        public bool UserExists(string login)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT COUNT(1) FROM EcoCerUsers WHERE Login = @Login";
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Login", login);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
               
            }
        }

        //Insertion du login dans la table EcoCerUsers
        //public void InsertLogin(string login)
        public bool InsertLogin(string login)
        {
            try
            {
                string query = @"INSERT INTO EcoCerUsers (Login, Nom, Prenom,Email)
						SELECT TOP(1) LOGIN, Nom, Prenom, Email FROM RHAccounts WHERE Login =@Login;";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                });
                return rowsAffected > 0;


            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when inserting in EcoCerUsers:", ex);
                throw new Exception("An error occured while inserting data in EcoCerUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in InsertLogin", ex);
                throw;
            }
        }



        public bool InsertCertificateDataModel(EcoCerCertificateDataModel model)
        {
            try
            {
                string query = @"INSERT INTO EcoCerCertificateDataModels(UserLogin, Nom, Prenom, Sexe, Civilite, CategorieProfessionnelle, DateRecrutement,CreationDate, CreationYear)
						VALUES(@UserLogin, @Nom, @Prenom, @Sexe, @Civilite, @CategorieProfessionnelle, @DateRecrutement, @CreationDate, @CreationYear);";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserLogin", model.Login);
                    cmd.Parameters.AddWithValue("@Nom", model.Nom);
                    cmd.Parameters.AddWithValue("@Prenom", model.Prenom);
                    cmd.Parameters.AddWithValue("@Sexe", model.Sexe);
                    cmd.Parameters.AddWithValue("@Civilite", model.Civilite);
                    cmd.Parameters.AddWithValue("@CategorieProfessionnelle", model.CategorieProfessionnelle);
                    cmd.Parameters.AddWithValue("@DateRecrutement", model.DateRecrutement);
                    cmd.Parameters.AddWithValue("@CreationDate", DateTime.Today);
                    //cmd.Parameters.AddWithValue("@CreationDate", DateTime.Today.ToString("dd/MM/yyyy"));
                    //cmd.Parameters.AddWithValue("@CreationDate", DateTime.Now.ToString("dd/MM/yyyy"));
                    cmd.Parameters.AddWithValue("@CreationYear", DateTime.Now.Year);
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when inserting in CertificateDataModel:", ex);
                throw new Exception("An error occured while inserting data in CertificateDataModel");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in InsertCertificateDataModel", ex);
                throw;
            }
        }

        //public bool UpdateCertificateDataModel(string login, string nom, string prenom, string sexe, string civilite)
        public bool UpdateCertificateDataModel(string login, string nom, string prenom, string sexe, string civilite, string DateRecrutement, string CategorieProfessionnelle)

        {
            try
            {
                string query = @"UPDATE EcoCerCertificateDataModels
					SET Nom = @Nom, Prenom = @Prenom, Sexe = @Sexe, Civilite = @Civilite, CategorieProfessionnelle = @CategorieProfessionnelle, DateRecrutement = @DateRecrutement, CreationDate = @CreationDate, CreationYear = @CreationYear
					WHERE UserLogin = @UserLogin;";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@UserLogin", login);
                    cmd.Parameters.AddWithValue("@Nom", nom);
                    cmd.Parameters.AddWithValue("@Prenom", prenom);
                    cmd.Parameters.AddWithValue("@Sexe", sexe);
                    cmd.Parameters.AddWithValue("@Civilite", civilite);
                    cmd.Parameters.AddWithValue("@CategorieProfessionnelle", CategorieProfessionnelle);
                    cmd.Parameters.AddWithValue("@DateRecrutement", DateRecrutement);
                    cmd.Parameters.AddWithValue("@CreationDate", DateTime.Today);
                    cmd.Parameters.AddWithValue("@CreationYear", DateTime.Today.Year);
                    
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when updating CertificateDataModel:", ex);
                throw new Exception("An error occured while updating CertificateDataModel");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in UpdateCertificateDataModel", ex);
                throw;
            }
        }

        public bool UpdateCertificateStatus(string status, int cdmId)
        {
            try
            {

                string query = "UPDATE EcoCerCertificateDataModels SET Statut = @Statut WHERE CdmId = @CdmId";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Statut", status);
                    cmd.Parameters.AddWithValue("@CdmId", cdmId);
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when updating Certificate status:", ex);
                throw new Exception("An error occured while updating a certificate status");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in UpdateCertificateStatus", ex);
                throw;
            }
        }

        //public int? GetLastCertificateRefNumber()
        //{
        //    try
        //    {
        //        string query = "SELECT TOP(1) RefNumber FROM EcoCerCertificateDataModels ORDER BY RefNumber DESC";

        //        var result = ExecuteScalar(query, cmd => { });

        //        if (result.ToString() == "000" || string.IsNullOrWhiteSpace(result.ToString()))
        //        {
        //            return null;
        //        }
        //        return int.Parse(result.ToString());
        //    }
        //    catch (SqlException ex)
        //    {
        //        _logger.LogError("Database error when getting the last certificate reference number:", ex);
        //        throw new Exception("An error occurer while fetching the last certificate reference number");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Unexpected error in GetLastCertificateRefNumber", ex);
        //        throw;
        //    }
        //}

        //public bool AddCertificateRefNumber(string login, string refNumber)
        //{
        //    string query = "UPDATE EcoCerCertificateDataModels SET RefNumber = @RefNumber WHERE UserLogin = @UserLogin";

        //    int rowsAffected = ExecuteNonQuery(query, cmd =>
        //    {
        //        cmd.Parameters.AddWithValue("@RefNumber", refNumber);
        //        cmd.Parameters.AddWithValue("@UserLogin", login);
        //    });

        //    return rowsAffected > 0;
        //}

        public bool AddCertificateRefNumberAndStatus(string login, string refNumber, string statut)
        {
            try
            {

                string query = "UPDATE EcoCerCertificateDataModels SET RefNumber = @RefNumber, Statut = @Statut WHERE UserLogin = @UserLogin";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@RefNumber", refNumber);
                    cmd.Parameters.AddWithValue("@Statut", statut);
                    cmd.Parameters.AddWithValue("@UserLogin", login);
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when adding refNumber and status:", ex);
                throw new Exception("An error occured while adding refNumber and status", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in AddCertificateRefNumberAndStatus", ex);
                throw;
            }
        }

        public bool UpdateCertificate(EcoCerCertificateDataModel model)
        {
            try
            {
                string query = @"UPDATE EcoCerCertificateDataModels
						SET Nom = @Nom, Prenom = @Prenom, Sexe = @Sexe, Civilite = @Civilite, CategorieProfessionnelle = @CategorieProfessionnelle, 
                     DateRecrutement = @DateRecrutement
						WHERE CdmId = @CdmId;";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Nom", model.Nom);
                    cmd.Parameters.AddWithValue("@Prenom", model.Prenom);
                    cmd.Parameters.AddWithValue("@Sexe", model.Sexe);
                    cmd.Parameters.AddWithValue("@Civilite", model.Civilite);
                    cmd.Parameters.AddWithValue("@CategorieProfessionnelle", model.CategorieProfessionnelle);
                    cmd.Parameters.AddWithValue("@DateRecrutement", model.DateRecrutement);
                    cmd.Parameters.AddWithValue("@CdmId", model.CdmId);
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when updating CertificateDataModel:", ex);
                throw new Exception("An error occured while updating CertificateDataModel", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in UpdateCertificateDataModel", ex);
                throw;
            }
        }

        public EcoCerCertificateTemplate? GetTemplate()
        {
            try
            {
                string query = "SELECT * FROM EcoCerCertificateTemplate WHERE CerTempId = 1";

                using (var reader = ExecuteReader(query, cmd => { }))
                {
                    if (reader.Read())
                    {
                        return new EcoCerCertificateTemplate
                        {
                            CerTempId = Convert.ToInt32(reader["CerTempId"]),
                            HeaderText = reader["HeaderText"].ToString(),
                            TitleText = reader["TitleText"].ToString(),
                            BodyTextPart1 = reader["BodyTextPart1"].ToString(),
                            BodyTextPart2 = reader["BodyTextPart2"].ToString(),
                            BodyTextPart3 = reader["BodyTextPart3"].ToString(),
                            BodyTextPart4 = reader["BodyTextPart4"].ToString(),
                            BodyTextPart5 = reader["BodyTextPart5"].ToString(),
                            DeliverDateText = reader["DeliverDateText"].ToString(),
                            FooterTextPart1 = reader["FooterTextPart1"].ToString(),
                            FooterTextPart2 = reader["FooterTextPart2"].ToString()
                        };
                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when getting EcoCerCertificateTemplate:", ex);
                throw new Exception("An error occured while getting a certificate template", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetTemplate", ex);
                throw;
            }
        }

        public bool UpdateCertificateTemplate(EcoCerTempViewModel templateViewModel)
        {
            try
            {
                string query = @"UPDATE EcoCerCertificateTemplate
                          SET HeaderText = @HeaderText, TitleText = @TitleText, BodyTextPart1 = @BodyTextPart1, BodyTextPart4 = @BodyTextPart4, BodyTextPart5 = @BodyTextPart5,
                          FooterTextPart1 = @FooterTextPart1, FooterTextPart2 = @FooterTextPart2 WHERE CerTempId = 1";

                int rowsAffected = ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@HeaderText", templateViewModel.HeaderText);
                    cmd.Parameters.AddWithValue("@TitleText", templateViewModel.TitleText);
                    cmd.Parameters.AddWithValue("@BodyTextPart1", templateViewModel.BodyTextPart1);
                    cmd.Parameters.AddWithValue("@BodyTextPart4", templateViewModel.BodyTextPart2);
                    cmd.Parameters.AddWithValue("@BodyTextPart5", templateViewModel.BodyTextPart3);
                    cmd.Parameters.AddWithValue("@FooterTextPart1", templateViewModel.FooterTextPart1);
                    cmd.Parameters.AddWithValue("@FooterTextPart2", templateViewModel.FooterTextPart2);
                    /*cmd.Parameters.AddWithValue("@CerTempId", template.CerTempId);*/
                });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when updating EcoCerCertificateTemplate:", ex);
                throw new Exception("An error occured while updating a certificate template", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in UpdateCertificateTemplate", ex);
                throw;
            }
        }

        


        //Reinitialisation d'EcoCerStaffInfos
        public bool ResetStaffInfos()
        {
            try
            {
                string query = @"DELETE FROM EcoCerStaffInfos;
                        DBCC CHECKIDENT('EcoCerStaffInfos', RESEED, 1)";

                int rowsAffected = ExecuteNonQuery(query, cmd => { });

                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when resetting staff informations:", ex);
                throw new Exception("An error occured while resetting staff informations", ex);

            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in ResetStaffInfos", ex);
                throw;

            }
        }

        //Insertion dans la table EcoCerStaffInfos
        public void InsertStaffInfos(string email, string numeroCompte, string categorieProfessionnelle, string dateRecrutement, string matricule)
        {
            try
            {

                string query = @"INSERT INTO EcoCerStaffInfos (Email, NumeroCompte, CategorieProfessionnelle, DateRecrutement, Matricule)
                    VALUES (@Email, @NumeroCompte, @CategorieProfessionnelle, @DateRecrutement, @Matricule)";

                ExecuteNonQuery(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@NumeroCompte", numeroCompte);
                    cmd.Parameters.AddWithValue("@CategorieProfessionnelle", categorieProfessionnelle);
                    cmd.Parameters.AddWithValue("@DateRecrutement", dateRecrutement);
                    cmd.Parameters.AddWithValue("@Matricule", matricule);
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when inserting staff account informations:", ex);
                throw new Exception("An error occured while staff account information", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in InsertEcoCerStaffInfo", ex);
                throw;
            }
        }

        //GetUserByLogin mis à jour
        public EcoCerUserDetails? GetUserByLogin(string login)
        {
            try
            {
                //EcoCerUser query
                //string query = "SELECT TOP(1) * FROM EcoCerUsers WHERE Login = @Login";

                //RHAccounts query
                //string query = "SELECT TOP(1) IDUser,Login,Nom,Prenom FROM RHAccounts where login= @Login";

                string query = @"SELECT TOP(1) r.IDUser,r.Login,r.Nom,r.Prenom, e.CategorieProfessionnelle, e.DateRecrutement FROM RHAccounts r 
                        JOIN EcoCerStaffInfos e 
                        ON r.Email = e.Email
                        where r.login= @Login";

                using (var reader = ExecuteReader(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                }))
                {
                    if (reader.Read())
                    {
                        /*return new EcoCerUserDetails
                        {
                            Login = reader["Login"].ToString(),
                            Nom = reader["Nom"].ToString(),
                            Prenom = reader["Prenom"].ToString(),
                            //Sexe = reader["Sexe"].ToString(),
                            //Civilite = reader["Civilite"].ToString(),
                            //CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                            //DateRecrutement = reader["DateRecrutement"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateRecrutement"]).Date
                        };*/

                        return new EcoCerUserDetails
                        {
                            Login = reader["Login"].ToString(),
                            Nom = reader["Nom"].ToString(),
                            Prenom = reader["Prenom"].ToString(),
                            CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                            DateRecrutement = reader["DateRecrutement"].ToString(),
                            //Sexe = reader["Sexe"].ToString(),
                            //Civilite = reader["Civilite"].ToString(),
                            //CategorieProfessionnelle = reader["CategorieProfessionnelle"].ToString(),
                            //DateRecrutement = reader["DateRecrutement"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DateRecrutement"]).Date
                        };
                    }
                }

                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when fetching user by Login:", ex);
                throw new Exception("An error occured while fetching a user");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetUserByLogin", ex);
                throw;
            }
        }

        public string? GetUserMailByCertificateId(int certificateId)
        {
            try
            {

                string query = @"SELECT Email AS UserMail FROM EcoCerUsers u
                     INNER JOIN EcoCerCertificateDataModels c ON c.UserLogin = u.Login
                     WHERE c.CdmId = @CertificateId";

                var result = ExecuteScalar(query, cmd =>
                {
                    cmd.Parameters.AddWithValue("@CertificateId", certificateId);
                });

                if (string.IsNullOrEmpty(result.ToString()))
                {
                    return null;
                }

                return result.ToString();
            }
            catch (SqlException ex)
            {
                _logger.LogError("Database error when getting user email:", ex);
                throw new Exception("An error occured while getting the user mail based on the certificate id", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error in GetUserMailByCertificateId", ex);
                throw;
            }
        }
    }
}
