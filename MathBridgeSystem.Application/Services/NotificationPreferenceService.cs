using MathBridgeSystem.Application.DTOs.NotificationPreference;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly INotificationPreferenceRepository _preferenceRepository;
        private readonly IUserRepository _userRepository;

        public NotificationPreferenceService(
            INotificationPreferenceRepository preferenceRepository,
            IUserRepository userRepository)
        {
            _preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<NotificationPreferenceDto> GetPreferencesByUserIdAsync(Guid userId)
        {
            var preference = await _preferenceRepository.GetByUserIdAsync(userId);
            if (preference == null)
                throw new KeyNotFoundException($"Notification preferences for user {userId} not found.");

            return MapToDto(preference);
        }

        public async Task<Guid> CreateOrUpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found.");

            var existingPreference = await _preferenceRepository.GetByUserIdAsync(userId);

            if (existingPreference == null)
            {
                // Create new preferences
                var newPreference = new NotificationPreference
                {
                    PreferenceId = Guid.NewGuid(),
                    UserId = userId,
                    ReceiveEmailNotifications = request.ReceiveEmailNotifications ?? true,
                    ReceiveSmsnotifications = request.ReceiveSmsNotifications ?? true,
                    ReceiveWebNotifications = request.ReceiveWebNotifications ?? true,
                    ReceiveSessionReminders = request.ReceiveSessionReminders ?? true,
                    ReceiveContractUpdates = request.ReceiveContractUpdates ?? true,
                    ReceivePaymentNotifications = request.ReceivePaymentNotifications ?? true,
                    CreatedDate = DateTime.UtcNow
                };

                await _preferenceRepository.AddAsync(newPreference);
                return newPreference.PreferenceId;
            }
            else
            {
                // Update existing preferences
                if (request.ReceiveEmailNotifications.HasValue)
                    existingPreference.ReceiveEmailNotifications = request.ReceiveEmailNotifications.Value;

                if (request.ReceiveSmsNotifications.HasValue)
                    existingPreference.ReceiveSmsnotifications = request.ReceiveSmsNotifications.Value;

                if (request.ReceiveWebNotifications.HasValue)
                    existingPreference.ReceiveWebNotifications = request.ReceiveWebNotifications.Value;

                if (request.ReceiveSessionReminders.HasValue)
                    existingPreference.ReceiveSessionReminders = request.ReceiveSessionReminders.Value;

                if (request.ReceiveContractUpdates.HasValue)
                    existingPreference.ReceiveContractUpdates = request.ReceiveContractUpdates.Value;

                if (request.ReceivePaymentNotifications.HasValue)
                    existingPreference.ReceivePaymentNotifications = request.ReceivePaymentNotifications.Value;

                existingPreference.UpdatedDate = DateTime.UtcNow;

                await _preferenceRepository.UpdateAsync(existingPreference);
                return existingPreference.PreferenceId;
            }
        }

        public async Task<NotificationPreferenceDto> GetOrCreateDefaultPreferencesAsync(Guid userId)
        {
            var preference = await _preferenceRepository.GetByUserIdAsync(userId);

            if (preference == null)
            {
                // Create default preferences
                var defaultPreference = new NotificationPreference
                {
                    PreferenceId = Guid.NewGuid(),
                    UserId = userId,
                    ReceiveEmailNotifications = true,
                    ReceiveSmsnotifications = true,
                    ReceiveWebNotifications = true,
                    ReceiveSessionReminders = true,
                    ReceiveContractUpdates = true,
                    ReceivePaymentNotifications = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _preferenceRepository.AddAsync(defaultPreference);
                return MapToDto(defaultPreference);
            }

            return MapToDto(preference);
        }

        private NotificationPreferenceDto MapToDto(NotificationPreference preference)
        {
            return new NotificationPreferenceDto
            {
                PreferenceId = preference.PreferenceId,
                UserId = preference.UserId,
                ReceiveEmailNotifications = preference.ReceiveEmailNotifications,
                ReceiveSmsNotifications = preference.ReceiveSmsnotifications,
                ReceiveWebNotifications = preference.ReceiveWebNotifications,
                ReceiveSessionReminders = preference.ReceiveSessionReminders,
                ReceiveContractUpdates = preference.ReceiveContractUpdates,
                ReceivePaymentNotifications = preference.ReceivePaymentNotifications,
                CreatedDate = preference.CreatedDate,
                UpdatedDate = preference.UpdatedDate
            };
        }
    }
}